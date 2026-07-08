using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Services;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Pós-processamento de dados de carga: turnos, faturamento completo e auditoria de simulação.
/// </summary>
public static class HospitalSimulationSeeder
{
    private static readonly (FinancialAccountCategory Category, string Label)[] ProposalLabels =
    [
        (FinancialAccountCategory.Consultation, "consulta ambulatorial"),
        (FinancialAccountCategory.Exam, "pacote de exames"),
        (FinancialAccountCategory.Hospitalization, "internação e diárias"),
        (FinancialAccountCategory.Copayment, "coparticipação convênio")
    ];

    public static async Task<HospitalSimulationResult> RunAsync(
        AppDbContext db,
        IFinancialAccountService financialAccountService,
        HospitalLoadDataOptions options,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!HospitalLoadDataSeeder.IsLoadSeedAllowed())
        {
            throw new InvalidOperationException(
                "Simulação bloqueada. Defina GTH_ALLOW_LOAD_SEED=true e use um banco de TESTE dedicado.");
        }

        var started = DateTime.UtcNow;
        var result = new HospitalSimulationResult();
        var rnd = new Random(options.RandomSeed + 9001);
        var simulationStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-options.SimulationDays));
        var marker = HospitalLoadDataOptions.MarkerPrefix;

        var markedPatientIds = await db.Patients
            .AsNoTracking()
            .Where(p => p.Notes != null && p.Notes.StartsWith(marker))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (markedPatientIds.Count == 0)
        {
            logger.LogWarning("Nenhum paciente marcado ({Marker}) para simulação.", marker);
            result.Elapsed = DateTime.UtcNow - started;
            return result;
        }

        await HospitalLoadDataSeeder.RefreshTodayQueuesAsync(db, options, logger, cancellationToken);

        result.EmployeeShiftsCreated = await SeedEmployeeShiftsAsync(
            db, simulationStart, rnd, logger, cancellationToken);

        result.FinancialAccountsCreated = await SeedCompletedAppointmentBillingAsync(
            db, financialAccountService, markedPatientIds, marker, logger, cancellationToken);

        var receivableStats = await SeedReceivableProposalsAsync(
            db, markedPatientIds, rnd, marker, logger, cancellationToken);
        result.ProposalsCreated = receivableStats.AccountsCreated;
        result.FinancialPaymentsCreated = receivableStats.PaymentsCreated;
        result.LineItemsCreated = receivableStats.LineItemsCreated;

        result.PayablesCreated = await SeedPayablesAsync(
            db, rnd, marker, logger, cancellationToken);

        var tissStats = await SeedTissBillingAsync(
            db, markedPatientIds, rnd, marker, logger, cancellationToken);
        result.TissGuidesCreated = tissStats.GuidesCreated;
        result.TissBatchesCreated = tissStats.BatchesCreated;
        result.FinancialAccountsCreated += tissStats.FinancialAccountsCreated;

        result.AuditLogsCreated = await SeedSchedulingAuditAsync(
            db, markedPatientIds, options, result, logger, cancellationToken);

        await HospitalOperationsSimulationSeeder.RunAsync(
            db, markedPatientIds, options, rnd, marker, result, logger, cancellationToken);

        await PopulateValidationCountsAsync(db, markedPatientIds, marker, result, cancellationToken);

        result.Elapsed = DateTime.UtcNow - started;
        return result;
    }

    private static async Task<int> SeedEmployeeShiftsAsync(
        AppDbContext db,
        DateOnly simulationStart,
        Random rnd,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var employees = await db.Employees
            .AsNoTracking()
            .Select(e => new { e.Id, e.DepartmentId })
            .ToListAsync(cancellationToken);

        if (employees.Count == 0)
        {
            logger.LogInformation("Nenhum funcionário cadastrado — turnos não gerados.");
            return 0;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existingKeys = await db.EmployeeShifts
            .AsNoTracking()
            .Where(s => s.ShiftDate >= simulationStart)
            .Select(s => new { s.EmployeeId, s.ShiftDate, s.ShiftType })
            .ToListAsync(cancellationToken);

        var existingSet = existingKeys
            .Select(k => (k.EmployeeId, k.ShiftDate, k.ShiftType))
            .ToHashSet();

        var shifts = new List<EmployeeShift>();
        var shiftTypes = new[] { ShiftType.Morning, ShiftType.Afternoon, ShiftType.Night };

        for (var date = simulationStart; date <= today; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            foreach (var employee in employees)
            {
                if (rnd.NextDouble() > 0.85)
                {
                    continue;
                }

                var shiftType = shiftTypes[rnd.Next(shiftTypes.Length)];
                if (existingSet.Contains((employee.Id, date, shiftType)))
                {
                    continue;
                }

                shifts.Add(new EmployeeShift
                {
                    EmployeeId = employee.Id,
                    DepartmentId = employee.DepartmentId,
                    ShiftDate = date,
                    ShiftType = shiftType
                });
            }
        }

        if (shifts.Count == 0)
        {
            return 0;
        }

        db.EmployeeShifts.AddRange(shifts);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Simulação: {Count} turnos de funcionários gerados.", shifts.Count);
        return shifts.Count;
    }

    private static async Task<int> SeedCompletedAppointmentBillingAsync(
        AppDbContext db,
        IFinancialAccountService financialAccountService,
        IReadOnlyList<Guid> markedPatientIds,
        string marker,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var completedAppointments = await db.Appointments
            .AsNoTracking()
            .Where(a => markedPatientIds.Contains(a.PatientId)
                && a.Status == AppointmentStatus.Completed
                && a.Notes != null
                && a.Notes.StartsWith(marker)
                && !db.FinancialAccounts.Any(f => f.AppointmentId == a.Id))
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        if (completedAppointments.Count == 0)
        {
            return 0;
        }

        var created = 0;

        foreach (var appointmentId in completedAppointments)
        {
            await financialAccountService.CreateFromAppointmentAsync(appointmentId, 250m, cancellationToken);
            created++;

            var account = await db.FinancialAccounts
                .FirstOrDefaultAsync(f => f.AppointmentId == appointmentId && f.IsActive, cancellationToken);
            if (account is not null && string.IsNullOrEmpty(account.Notes))
            {
                account.Notes = marker;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        logger.LogInformation("Simulação: {Count} contas de consultas concluídas geradas.", created);
        return created;
    }

    private static async Task<(int AccountsCreated, int PaymentsCreated, int LineItemsCreated)> SeedReceivableProposalsAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        Random rnd,
        string marker,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var proposalPatientCount = Math.Max(1, (int)(markedPatientIds.Count * 0.35));
        var selectedPatients = markedPatientIds.OrderBy(_ => rnd.Next()).Take(proposalPatientCount).ToList();

        var accounts = new List<FinancialAccount>();
        var pendingPayments = new List<(FinancialAccount Account, decimal Amount, PaymentMethod Method)>();
        var lineItemTarget = 0;
        var paymentMethods = new[] { PaymentMethod.Pix, PaymentMethod.CreditCard, PaymentMethod.DebitCard, PaymentMethod.Cash };

        foreach (var patientId in selectedPatients)
        {
            var proposalCount = rnd.NextDouble() < 0.25 ? 2 : 1;
            for (var p = 0; p < proposalCount; p++)
            {
                var label = ProposalLabels[rnd.Next(ProposalLabels.Length)];
                var amount = rnd.Next(150, 2501);
                var roll = rnd.NextDouble();

                FinancialAccountStatus status;
                decimal paidAmount = 0;
                DateTime? paidAt = null;
                DateTime dueDate;

                if (roll < 0.30)
                {
                    status = FinancialAccountStatus.Open;
                    dueDate = DateTime.UtcNow.AddDays(rnd.Next(5, 45));
                }
                else if (roll < 0.45)
                {
                    status = FinancialAccountStatus.Open;
                    dueDate = DateTime.UtcNow.AddDays(-rnd.Next(3, 60));
                }
                else if (roll < 0.70)
                {
                    status = FinancialAccountStatus.PartiallyPaid;
                    paidAmount = Math.Round(amount * (decimal)(0.2 + rnd.NextDouble() * 0.5), 2);
                    dueDate = DateTime.UtcNow.AddDays(rnd.Next(-10, 30));
                }
                else
                {
                    status = FinancialAccountStatus.Paid;
                    paidAmount = amount;
                    paidAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 30));
                    dueDate = paidAt.Value;
                }

                var account = new FinancialAccount
                {
                    Direction = FinancialAccountDirection.Receivable,
                    PatientId = patientId,
                    Category = label.Category,
                    Description = $"Proposta — orçamento {label.Label}",
                    Amount = amount,
                    PaidAmount = paidAmount,
                    Status = status,
                    DueDate = dueDate,
                    PaidAt = paidAt,
                    Notes = marker
                };

                accounts.Add(account);

                if (paidAmount > 0)
                {
                    pendingPayments.Add((account, paidAmount, paymentMethods[rnd.Next(paymentMethods.Length)]));
                }

                if (rnd.NextDouble() < 0.35)
                {
                    var itemCount = rnd.Next(2, 4);
                    var unitBase = decimal.Round((decimal)amount / itemCount, 2);
                    for (var i = 0; i < itemCount; i++)
                    {
                        account.LineItems.Add(new FinancialAccountLineItem
                        {
                            Description = $"Item {i + 1} — {label.Label}",
                            Quantity = 1,
                            UnitAmount = unitBase,
                            TotalAmount = unitBase,
                            Notes = marker
                        });
                        lineItemTarget++;
                    }
                }
            }
        }

        if (accounts.Count == 0)
        {
            return (0, 0, 0);
        }

        db.FinancialAccounts.AddRange(accounts);
        await db.SaveChangesAsync(cancellationToken);

        var paymentsCreated = 0;
        foreach (var (account, amount, method) in pendingPayments)
        {
            var paidAt = account.PaidAt ?? DateTime.UtcNow.AddDays(-rnd.Next(1, 20));
            db.FinancialPayments.Add(new FinancialPayment
            {
                FinancialAccountId = account.Id,
                Amount = amount,
                Method = method,
                PaidAt = paidAt,
                Notes = marker
            });
            paymentsCreated++;
        }

        if (paymentsCreated > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "Simulação: {Accounts} propostas, {Payments} pagamentos, {Items} itens de linha.",
            accounts.Count,
            paymentsCreated,
            lineItemTarget);

        return (accounts.Count, paymentsCreated, lineItemTarget);
    }

    private static async Task<int> SeedPayablesAsync(
        AppDbContext db,
        Random rnd,
        string marker,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var suppliers = await db.Suppliers.AsNoTracking().ToListAsync(cancellationToken);
        var markedSupplier = suppliers.FirstOrDefault(s => s.Name.Contains("GTH", StringComparison.OrdinalIgnoreCase));

        if (markedSupplier is null && suppliers.Count > 0)
        {
            markedSupplier = new Supplier
            {
                Name = "GTH Fornecedor Simulação",
                Cnpj = "00000000000199",
                Email = "fornecedor@gth-load.local",
                Phone = HospitalLoadDataSeeder.GenerateLandlinePhone(rnd),
                ContactName = "Contato GTH"
            };
            db.Suppliers.Add(markedSupplier);
            await db.SaveChangesAsync(cancellationToken);
            suppliers.Add(markedSupplier);
        }

        var payables = new List<FinancialAccount>();
        var paymentMethods = new[] { PaymentMethod.BankTransfer, PaymentMethod.Pix };

        if (suppliers.Count > 0)
        {
            var supplier = suppliers[rnd.Next(suppliers.Count)];
            payables.Add(CreatePayable(
                FinancialAccountCategory.SupplierPurchase,
                supplier.Id,
                null,
                $"Pedido de compras — {supplier.Name}",
                rnd.Next(1200, 8500),
                marker,
                rnd));
        }

        payables.Add(CreatePayable(
            FinancialAccountCategory.Utilities,
            null,
            "Companhia de Energia",
            "Conta de energia elétrica — unidade principal",
            Math.Round((decimal)(rnd.NextDouble() * 4000 + 800), 2),
            marker,
            rnd));

        payables.Add(CreatePayable(
            FinancialAccountCategory.Payroll,
            null,
            "Folha de pagamento",
            "Folha de pagamento — equipe assistencial",
            rnd.Next(120000, 220000),
            marker,
            rnd,
            forcePaid: true));

        payables.Add(CreatePayable(
            FinancialAccountCategory.Maintenance,
            null,
            "Manutenção predial",
            "Manutenção de equipamentos e infraestrutura",
            rnd.Next(800, 6500),
            marker,
            rnd));

        db.FinancialAccounts.AddRange(payables);
        await db.SaveChangesAsync(cancellationToken);

        var payments = new List<FinancialPayment>();
        foreach (var payable in payables.Where(p => p.PaidAmount > 0))
        {
            payments.Add(new FinancialPayment
            {
                FinancialAccountId = payable.Id,
                Amount = payable.PaidAmount,
                Method = paymentMethods[rnd.Next(paymentMethods.Length)],
                PaidAt = payable.PaidAt ?? DateTime.UtcNow.AddDays(-rnd.Next(1, 10)),
                Notes = marker
            });
        }

        if (payments.Count > 0)
        {
            db.FinancialPayments.AddRange(payments);
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Simulação: {Count} contas a pagar geradas.", payables.Count);
        return payables.Count;
    }

    private static FinancialAccount CreatePayable(
        FinancialAccountCategory category,
        Guid? supplierId,
        string? counterpartyName,
        string description,
        decimal amount,
        string marker,
        Random rnd,
        bool forcePaid = false)
    {
        var roll = forcePaid ? 1.0 : rnd.NextDouble();
        FinancialAccountStatus status;
        decimal paidAmount = 0;
        DateTime? paidAt = null;

        if (roll < 0.40)
        {
            status = FinancialAccountStatus.Open;
        }
        else if (roll < 0.65)
        {
            status = FinancialAccountStatus.PartiallyPaid;
            paidAmount = Math.Round(amount * (decimal)(0.3 + rnd.NextDouble() * 0.4), 2);
        }
        else
        {
            status = FinancialAccountStatus.Paid;
            paidAmount = amount;
            paidAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 15));
        }

        return new FinancialAccount
        {
            Direction = FinancialAccountDirection.Payable,
            SupplierId = supplierId,
            CounterpartyName = counterpartyName,
            Category = category,
            Description = description,
            Amount = amount,
            PaidAmount = paidAmount,
            Status = status,
            DueDate = DateTime.UtcNow.AddDays(rnd.Next(-5, 30)),
            PaidAt = paidAt,
            Notes = marker
        };
    }

    private static async Task<(int GuidesCreated, int BatchesCreated, int FinancialAccountsCreated)> SeedTissBillingAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        Random rnd,
        string marker,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var insuranceAppointments = await db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.Insurances).ThenInclude(i => i.HealthInsurance)
            .Where(a => markedPatientIds.Contains(a.PatientId)
                && a.Status == AppointmentStatus.Completed
                && a.Notes != null
                && a.Notes.StartsWith(marker))
            .ToListAsync(cancellationToken);

        var alreadyGuidedAppointmentIds = await db.TissGuides
            .AsNoTracking()
            .Where(g => g.Notes != null && g.Notes.StartsWith(marker) && g.AppointmentId != null)
            .Select(g => g.AppointmentId!.Value)
            .ToListAsync(cancellationToken);
        var guidedSet = alreadyGuidedAppointmentIds.ToHashSet();

        var eligible = insuranceAppointments
            .Where(a => !guidedSet.Contains(a.Id))
            .Where(a =>
            {
                var insurance = a.Patient.Insurances.FirstOrDefault(i => i.IsPrimary) ?? a.Patient.Insurances.FirstOrDefault();
                if (insurance?.HealthInsurance is null)
                {
                    return false;
                }

                var name = insurance.HealthInsurance.Name;
                return !name.Equals("SUS", StringComparison.OrdinalIgnoreCase)
                    && !name.Equals("Particular", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(_ => rnd.Next())
            .ToList();

        var targetCount = Math.Max(1, (int)(eligible.Count * 0.10));
        var selected = eligible.Take(targetCount).ToList();

        if (selected.Count == 0)
        {
            return (0, 0, 0);
        }

        var guideCount = await db.TissGuides.CountAsync(cancellationToken);
        var guides = new List<TissGuide>();
        var linkedAccounts = 0;

        foreach (var appointment in selected)
        {
            var insurance = appointment.Patient.Insurances.FirstOrDefault(i => i.IsPrimary)
                ?? appointment.Patient.Insurances.First();
            guideCount++;
            var amount = rnd.Next(180, 1200);

            var guide = new TissGuide
            {
                GuideNumber = $"GTH-TISS-{DateTime.UtcNow:yyyy}-{guideCount:D6}",
                PatientId = appointment.PatientId,
                HealthInsuranceId = insurance.HealthInsuranceId,
                AppointmentId = appointment.Id,
                GuideType = TissGuideType.Consultation,
                Status = rnd.NextDouble() < 0.6 ? TissGuideStatus.Sent : TissGuideStatus.Draft,
                TotalAmount = amount,
                SentAt = rnd.NextDouble() < 0.6 ? DateTime.UtcNow.AddDays(-rnd.Next(1, 20)) : null,
                BeneficiaryCardNumber = insurance.CardNumber,
                BeneficiaryPlanName = insurance.PlanName,
                RequestingProfessionalId = appointment.ProfessionalId,
                ExecutingProfessionalId = appointment.ProfessionalId,
                Notes = marker,
                ClientRequestId = $"{marker}-guide-{appointment.Id:N}",
                Items =
                [
                    new TissGuideItem
                    {
                        TussCode = "10101012",
                        Description = "Consulta em consultório",
                        Quantity = 1,
                        UnitPrice = amount,
                        IsAudited = rnd.NextDouble() < 0.5
                    }
                ]
            };

            guides.Add(guide);
            db.TissGuides.Add(guide);

            var existingAccount = await db.FinancialAccounts
                .FirstOrDefaultAsync(f => f.AppointmentId == appointment.Id, cancellationToken);

            if (existingAccount is not null)
            {
                existingAccount.TissGuide = guide;
                existingAccount.HealthInsuranceId = insurance.HealthInsuranceId;
                existingAccount.Category = FinancialAccountCategory.InsuranceReceivable;
                existingAccount.Description = $"Faturamento TISS — guia {guide.GuideNumber}";
                existingAccount.Amount = amount;
                existingAccount.Notes = marker;
                linkedAccounts++;
            }
            else
            {
                db.FinancialAccounts.Add(new FinancialAccount
                {
                    Direction = FinancialAccountDirection.Receivable,
                    PatientId = appointment.PatientId,
                    AppointmentId = appointment.Id,
                    HealthInsuranceId = insurance.HealthInsuranceId,
                    Category = FinancialAccountCategory.InsuranceReceivable,
                    Description = $"Faturamento TISS — guia {guide.GuideNumber}",
                    Amount = amount,
                    Status = FinancialAccountStatus.Open,
                    DueDate = DateTime.UtcNow.AddDays(45),
                    Notes = marker,
                    TissGuide = guide
                });
                linkedAccounts++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var batchesCreated = 0;
        foreach (var insuranceGroup in guides.Where(g => g.Status == TissGuideStatus.Sent).GroupBy(g => g.HealthInsuranceId))
        {
            var batchCount = await db.TissBatches.CountAsync(cancellationToken);
            var competence = DateTime.UtcNow.ToString("yyyy-MM");
            var groupGuides = insuranceGroup.ToList();
            var batch = new TissBatch
            {
                BatchNumber = $"GTH-LOAD-{competence.Replace("-", "")}-{(batchCount + 1):D4}",
                HealthInsuranceId = insuranceGroup.Key,
                Competence = competence,
                Status = TissBatchStatus.Generated,
                TotalAmount = groupGuides.Sum(g => g.TotalAmount),
                GuideCount = groupGuides.Count
            };

            db.TissBatches.Add(batch);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var guide in groupGuides)
            {
                guide.TissBatchId = batch.Id;
            }

            await db.SaveChangesAsync(cancellationToken);
            batchesCreated++;
        }

        logger.LogInformation(
            "Simulação: {Guides} guias TISS, {Batches} lotes, {Accounts} contas vinculadas.",
            guides.Count,
            batchesCreated,
            linkedAccounts);

        return (guides.Count, batchesCreated, linkedAccounts);
    }

    private static async Task<int> SeedSchedulingAuditAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        HospitalLoadDataOptions options,
        HospitalSimulationResult result,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var markedAppointments = await db.Appointments
            .AsNoTracking()
            .Where(a => markedPatientIds.Contains(a.PatientId)
                && a.Notes != null
                && a.Notes.StartsWith(HospitalLoadDataOptions.MarkerPrefix))
            .Select(a => new
            {
                a.Id,
                a.ProfessionalId,
                a.ScheduledAt,
                a.DurationMinutes,
                a.Status,
                a.IsActive
            })
            .ToListAsync(cancellationToken);

        var conflicts = new List<(Guid Id, Guid ProfessionalId, DateTime ScheduledAt)>();

        var byProfessional = markedAppointments.GroupBy(a => a.ProfessionalId);
        foreach (var group in byProfessional)
        {
            var slots = group
                .Where(a => AppointmentSchedulingEngine.IsBlocking(a.Status, a.IsActive))
                .OrderBy(a => a.ScheduledAt)
                .ToList();

            for (var i = 0; i < slots.Count; i++)
            {
                for (var j = i + 1; j < slots.Count; j++)
                {
                    if (AppointmentSchedulingEngine.IntervalsOverlap(
                            slots[i].ScheduledAt,
                            slots[i].DurationMinutes,
                            slots[j].ScheduledAt,
                            slots[j].DurationMinutes))
                    {
                        conflicts.Add((slots[j].Id, group.Key, slots[j].ScheduledAt));
                    }
                }
            }
        }

        result.ScheduleConflicts = conflicts.Count;

        if (conflicts.Count == 0)
        {
            return 0;
        }

        var auditEntries = conflicts.Select(c => new AuditLog
        {
            UserEmail = "gth-simulation@load-seed.local",
            Action = "ScheduleConflictDetected",
            EntityType = "Appointment",
            EntityId = c.Id,
            Details = $"Conflito de agenda detectado na simulação (profissional {c.ProfessionalId}, horário {c.ScheduledAt:O}).",
            ActionCategory = "Simulation",
            IsSensitive = false
        }).ToList();

        db.AuditLogs.AddRange(auditEntries);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Simulação: {Count} conflitos de agenda registrados em AuditLog (UseSmartScheduling={Smart}).",
            auditEntries.Count,
            options.UseSmartScheduling);

        return auditEntries.Count;
    }

    private static async Task PopulateValidationCountsAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        string marker,
        HospitalSimulationResult result,
        CancellationToken cancellationToken)
    {
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        result.WaitingRoomTodayCount = await db.Appointments
            .AsNoTracking()
            .CountAsync(a => markedPatientIds.Contains(a.PatientId)
                && a.Notes != null
                && a.Notes.StartsWith(marker)
                && a.ScheduledAt >= todayStart
                && a.ScheduledAt < todayEnd
                && (a.Status == AppointmentStatus.Scheduled
                    || a.Status == AppointmentStatus.Confirmed
                    || a.Status == AppointmentStatus.InProgress),
                cancellationToken);

        result.FinancialAccountsTotal = await db.FinancialAccounts
            .AsNoTracking()
            .CountAsync(f =>
                (f.Notes != null && f.Notes.StartsWith(marker))
                || (f.PatientId != null && markedPatientIds.Contains(f.PatientId.Value)),
                cancellationToken);

        result.OpenProposalsCount = await db.FinancialAccounts
            .AsNoTracking()
            .CountAsync(f => f.IsActive
                && f.Direction == FinancialAccountDirection.Receivable
                && f.Status == FinancialAccountStatus.Open
                && (f.Description.StartsWith("Orçamento")
                    || f.Description.Contains("Proposta —"))
                && ((f.Notes != null && f.Notes.StartsWith(marker))
                    || (f.PatientId != null && markedPatientIds.Contains(f.PatientId.Value))),
                cancellationToken);

        result.ProductsTotal = await db.Products
            .AsNoTracking()
            .CountAsync(p => p.Sku.StartsWith(HospitalOperationsSimulationSeeder.SkuPrefix), cancellationToken);

        result.StockMovementsTotal = await db.StockMovements
            .AsNoTracking()
            .CountAsync(m => m.Reference != null && m.Reference.StartsWith(marker), cancellationToken);

        result.OpenCashSessionsCount = await db.FinancialCashSessions
            .AsNoTracking()
            .CountAsync(s => s.Notes != null && s.Notes.StartsWith(marker)
                && s.Status == FinancialCashSessionStatus.Open, cancellationToken);
    }
}

public sealed class HospitalSimulationResult
{
    public int EmployeeShiftsCreated { get; set; }
    public int FinancialAccountsCreated { get; set; }
    public int ProposalsCreated { get; set; }
    public int PayablesCreated { get; set; }
    public int FinancialPaymentsCreated { get; set; }
    public int LineItemsCreated { get; set; }
    public int TissGuidesCreated { get; set; }
    public int TissBatchesCreated { get; set; }
    public int AuditLogsCreated { get; set; }
    public int ProductsCreated { get; set; }
    public int StockMovementsCreated { get; set; }
    public int ProductBillingRulesCreated { get; set; }
    public int PharmacyDispensingsCreated { get; set; }
    public int PharmacyBillingEntriesCreated { get; set; }
    public int HonorariosCreated { get; set; }
    public int MiscellaneousReceiptsCreated { get; set; }
    public int CashSessionsCreated { get; set; }
    public int PayrollRunsCreated { get; set; }
    public int TpaClaimsCreated { get; set; }
    public int WaitingRoomTodayCount { get; set; }
    public int FinancialAccountsTotal { get; set; }
    public int OpenProposalsCount { get; set; }
    public int ScheduleConflicts { get; set; }
    public int ProductsTotal { get; set; }
    public int StockMovementsTotal { get; set; }
    public int OpenCashSessionsCount { get; set; }
    public TimeSpan Elapsed { get; set; }
}
