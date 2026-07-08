using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.Common;
using SistemaHospitalar.Application.DTOs.Financial;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class FinancialAccountService(AppDbContext dbContext) : IFinancialAccountService
{
    private const decimal ConsultationParticularAmount = 250m;
    private const decimal ConsultationCopaymentAmount = 85m;
    private const decimal ExamPackageAmount = 185.50m;
    private const decimal DefaultCopaymentAmount = 320m;

    public async Task<PagedResult<FinancialAccountDto>> SearchAsync(
        FinancialAccountStatus? status,
        FinancialAccountDirection? direction,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.FinancialAccounts.AsNoTracking().Where(f => f.IsActive);

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        if (direction.HasValue)
        {
            query = query.Where(f => f.Direction == direction.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(f =>
                f.Description.Contains(term) ||
                (f.Patient != null && f.Patient.FullName.Contains(term)) ||
                (f.Supplier != null && f.Supplier.Name.Contains(term)) ||
                (f.CounterpartyName != null && f.CounterpartyName.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDto())
            .ToListAsync(cancellationToken);

        return new PagedResult<FinancialAccountDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyList<FinancialAccountDto>> GetByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.FinancialAccounts
            .AsNoTracking()
            .Where(f => f.PatientId == patientId && f.IsActive && f.Direction == FinancialAccountDirection.Receivable)
            .OrderByDescending(f => f.CreatedAt)
            .Select(MapToDto())
            .ToListAsync(cancellationToken);
    }

    public IReadOnlyList<PayableCategoryPresetDto> GetPayableCategoryPresets() =>
    [
        new(FinancialAccountCategory.SupplierPurchase, "Compra / fornecedor", 47850.90m, "NF — material médico-hospitalar (pedido PC-2026-0142)", 30),
        new(FinancialAccountCategory.Payroll, "Folha de pagamento", 892450m, "Folha de pagamento — todos os colaboradores ativos", 5),
        new(FinancialAccountCategory.Utilities, "Utilidades (água, luz, telefone)", 68420m, "Energia elétrica — unidade hospitalar", 10),
        new(FinancialAccountCategory.Taxes, "Impostos e taxas", 24500m, "DARF e tributos retidos — competência mensal", 15),
        new(FinancialAccountCategory.Maintenance, "Manutenção e serviços", 15800m, "Contrato HVAC / manutenção predial", 15),
        new(FinancialAccountCategory.OtherExpense, "Outras despesas", 5200m, "Despesa operacional diversa", 15),
    ];

    public async Task<FinancialAccountCreateSuggestionsDto> GetCreateSuggestionsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.Patients
            .AsNoTracking()
            .Include(p => p.Insurances).ThenInclude(i => i.HealthInsurance)
            .FirstOrDefaultAsync(p => p.Id == patientId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Paciente não encontrado.");

        var primaryInsurance = patient.Insurances.FirstOrDefault(i => i.IsPrimary)
            ?? patient.Insurances.FirstOrDefault();
        var insuranceName = primaryInsurance?.HealthInsurance?.Name;
        var modality = ResolvePaymentModality(insuranceName);
        var dueDays = SuggestedDueDays(modality);

        var outstanding = await dbContext.FinancialAccounts
            .AsNoTracking()
            .Where(f => f.PatientId == patientId && f.IsActive && f.Direction == FinancialAccountDirection.Receivable
                && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid))
            .SumAsync(f => f.Amount - f.PaidAmount, cancellationToken);

        var billedAppointmentIds = await dbContext.FinancialAccounts
            .AsNoTracking()
            .Where(f => f.PatientId == patientId && f.IsActive && f.AppointmentId != null)
            .Select(f => f.AppointmentId!.Value)
            .ToListAsync(cancellationToken);

        var billedHospitalizationIds = await dbContext.FinancialAccounts
            .AsNoTracking()
            .Where(f => f.PatientId == patientId && f.IsActive && f.HospitalizationId != null)
            .Select(f => f.HospitalizationId!.Value)
            .ToListAsync(cancellationToken);

        var appointments = await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Professional).ThenInclude(p => p.Specialty)
            .Where(a => a.PatientId == patientId
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow)
            .OrderByDescending(a => a.ScheduledAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        var hospitalizations = await dbContext.Hospitalizations
            .AsNoTracking()
            .Include(h => h.Bed).ThenInclude(b => b.Ward)
            .Include(h => h.Professional)
            .Where(h => h.PatientId == patientId
                && (h.Status == HospitalizationStatus.Active || h.DischargedAt >= DateTime.UtcNow.AddDays(-30)))
            .OrderByDescending(h => h.AdmittedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var parkingSessions = await dbContext.ParkingSessions
            .AsNoTracking()
            .Include(s => s.ParkingZone)
            .Where(s => s.PatientId == patientId
                && !s.IsPaid
                && s.AmountCharged > 0
                && s.Status != ParkingSessionStatus.Active)
            .OrderByDescending(s => s.ExitedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var sourceOptions = new List<FinancialAccountSourceOptionDto>();

        foreach (var appointment in appointments)
        {
            var alreadyBilled = billedAppointmentIds.Contains(appointment.Id);
            var amount = modality == 1 ? ConsultationParticularAmount : ConsultationCopaymentAmount;
            var scheduledLocal = appointment.ScheduledAt.ToLocalTime();
            sourceOptions.Add(new FinancialAccountSourceOptionDto(
                "appointment",
                appointment.Id,
                $"Consulta — {appointment.Professional.Specialty.Name}",
                $"{scheduledLocal:dd/MM/yyyy HH:mm} · {appointment.Professional.FullName}",
                amount,
                $"Consulta {appointment.Professional.Specialty.Name} — {scheduledLocal:dd/MM/yyyy}",
                FinancialAccountCategory.Consultation,
                alreadyBilled));
        }

        foreach (var hospitalization in hospitalizations)
        {
            var alreadyBilled = billedHospitalizationIds.Contains(hospitalization.Id);
            var days = Math.Max(1, (int)Math.Ceiling((DateTime.UtcNow - hospitalization.AdmittedAt).TotalDays));
            var dailyRate = DailyRateForWard(hospitalization.Bed.Ward.Category);
            var amount = dailyRate * days;
            var wardName = hospitalization.Bed.Ward.Name;
            sourceOptions.Add(new FinancialAccountSourceOptionDto(
                "hospitalization",
                hospitalization.Id,
                $"Internação — {wardName}",
                $"{days} diária(s) · Leito {hospitalization.Bed.BedNumber} · Dr(a). {hospitalization.Professional.FullName}",
                amount,
                $"Internação {wardName} — {days} diária(s)",
                FinancialAccountCategory.Hospitalization,
                alreadyBilled));
        }

        foreach (var session in parkingSessions)
        {
            sourceOptions.Add(new FinancialAccountSourceOptionDto(
                "parking",
                session.Id,
                $"Estacionamento — {session.VehiclePlate}",
                $"{session.ParkingZone.Name} · R$ {session.AmountCharged:F2}",
                session.AmountCharged ?? 0m,
                $"Estacionamento {session.VehiclePlate} — {session.ParkingZone.Name}",
                FinancialAccountCategory.Parking,
                false));
        }

        var categoryPresets = BuildCategoryPresets(modality, insuranceName);

        return new FinancialAccountCreateSuggestionsDto(
            patient.Id,
            patient.FullName,
            patient.Cpf,
            patient.MobilePhone ?? patient.Phone,
            insuranceName,
            modality,
            dueDays,
            outstanding,
            sourceOptions,
            categoryPresets);
    }

    public async Task<FinancialAccountDto> CreateAsync(
        CreateFinancialAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var amount = ResolveAccountAmount(request);
        if (amount <= 0)
        {
            throw new InvalidOperationException("Valor deve ser maior que zero.");
        }

        var normalized = request with { Amount = amount };

        if (normalized.Direction == FinancialAccountDirection.Payable)
        {
            return await CreatePayableAsync(normalized, cancellationToken);
        }

        return await CreateReceivableAsync(normalized, cancellationToken);
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.FinancialAccounts.FirstOrDefaultAsync(
            f => f.Id == id && f.IsActive,
            cancellationToken);
        if (account is null)
        {
            return false;
        }

        if (account.PaidAmount > 0)
        {
            throw new InvalidOperationException("Não é possível cancelar conta com pagamentos registrados.");
        }

        account.Status = FinancialAccountStatus.Cancelled;
        account.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static decimal ResolveAccountAmount(CreateFinancialAccountRequest request)
    {
        if (request.LineItems is { Count: > 0 } items)
        {
            return items.Sum(i => i.Quantity * i.UnitAmount);
        }

        return request.Amount;
    }

    private async Task<FinancialAccountDto> CreateReceivableAsync(
        CreateFinancialAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.PatientId.HasValue)
        {
            throw new InvalidOperationException("Paciente é obrigatório para contas a receber.");
        }

        var patient = await dbContext.Patients
            .AsNoTracking()
            .Include(p => p.Insurances).ThenInclude(i => i.HealthInsurance)
            .FirstOrDefaultAsync(p => p.Id == request.PatientId && p.IsActive, cancellationToken);

        if (patient is null)
        {
            throw new InvalidOperationException("Paciente não encontrado.");
        }

        if (request.AppointmentId.HasValue)
        {
            var appointmentValid = await dbContext.Appointments.AnyAsync(
                a => a.Id == request.AppointmentId && a.PatientId == request.PatientId,
                cancellationToken);

            if (!appointmentValid)
            {
                throw new InvalidOperationException("Agendamento inválido para este paciente.");
            }

            var duplicateAppointment = await dbContext.FinancialAccounts.AnyAsync(
                f => f.AppointmentId == request.AppointmentId && f.IsActive,
                cancellationToken);

            if (duplicateAppointment)
            {
                throw new InvalidOperationException("Já existe conta financeira para este agendamento.");
            }
        }

        if (request.HospitalizationId.HasValue)
        {
            var hospitalizationValid = await dbContext.Hospitalizations.AnyAsync(
                h => h.Id == request.HospitalizationId && h.PatientId == request.PatientId,
                cancellationToken);

            if (!hospitalizationValid)
            {
                throw new InvalidOperationException("Internação inválida para este paciente.");
            }

            var duplicateHospitalization = await dbContext.FinancialAccounts.AnyAsync(
                f => f.HospitalizationId == request.HospitalizationId && f.IsActive,
                cancellationToken);

            if (duplicateHospitalization)
            {
                throw new InvalidOperationException("Já existe conta financeira para esta internação.");
            }
        }

        var insuranceName = patient.Insurances.FirstOrDefault(i => i.IsPrimary)?.HealthInsurance?.Name
            ?? patient.Insurances.FirstOrDefault()?.HealthInsurance?.Name;
        var modality = ResolvePaymentModality(insuranceName);
        var dueDate = request.DueDate?.ToUniversalTime()
            ?? DateTime.UtcNow.AddDays(SuggestedDueDays(modality, request.Category));

        var account = new FinancialAccount
        {
            Direction = FinancialAccountDirection.Receivable,
            PatientId = request.PatientId,
            AppointmentId = request.AppointmentId,
            HospitalizationId = request.HospitalizationId,
            Category = request.Category,
            Description = request.Description.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            InvoiceNumber = string.IsNullOrWhiteSpace(request.InvoiceNumber) ? null : request.InvoiceNumber.Trim(),
            Amount = request.Amount,
            DueDate = dueDate,
            ExpectedPaymentMethod = request.ExpectedPaymentMethod
        };

        return await PersistAccountAsync(account, request, cancellationToken);
    }

    private async Task<FinancialAccountDto> CreatePayableAsync(
        CreateFinancialAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PatientId.HasValue || request.AppointmentId.HasValue || request.HospitalizationId.HasValue)
        {
            throw new InvalidOperationException("Contas a pagar não podem ser vinculadas a paciente ou atendimento.");
        }

        var hasSupplier = request.SupplierId.HasValue;
        var hasCounterparty = !string.IsNullOrWhiteSpace(request.CounterpartyName);
        if (!hasSupplier && !hasCounterparty)
        {
            throw new InvalidOperationException("Informe o fornecedor ou o nome do favorecido.");
        }

        if (hasSupplier)
        {
            var supplierExists = await dbContext.Suppliers.AnyAsync(
                s => s.Id == request.SupplierId && s.IsActive,
                cancellationToken);

            if (!supplierExists)
            {
                throw new InvalidOperationException("Fornecedor não encontrado.");
            }
        }

        var preset = GetPayableCategoryPresets()
            .FirstOrDefault(p => p.Category == request.Category);
        var dueDate = request.DueDate?.ToUniversalTime()
            ?? DateTime.UtcNow.AddDays(preset?.SuggestedDueDays ?? 15);

        var account = new FinancialAccount
        {
            Direction = FinancialAccountDirection.Payable,
            SupplierId = request.SupplierId,
            CounterpartyName = string.IsNullOrWhiteSpace(request.CounterpartyName)
                ? null
                : request.CounterpartyName.Trim(),
            Category = request.Category,
            Description = request.Description.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            InvoiceNumber = string.IsNullOrWhiteSpace(request.InvoiceNumber) ? null : request.InvoiceNumber.Trim(),
            Amount = request.Amount,
            DueDate = dueDate,
            ExpectedPaymentMethod = request.ExpectedPaymentMethod
        };

        return await PersistAccountAsync(account, request, cancellationToken);
    }

    private async Task<FinancialAccountDto> PersistAccountAsync(
        FinancialAccount account,
        CreateFinancialAccountRequest request,
        CancellationToken cancellationToken)
    {
        var installmentCount = Math.Clamp(request.InstallmentCount ?? 1, 1, 48);

        if (installmentCount <= 1)
        {
            dbContext.FinancialAccounts.Add(account);
            AddLineItems(account, request.LineItems);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (account.Direction == FinancialAccountDirection.Receivable)
            {
                await ScheduleBillingReminderAsync(account, cancellationToken);
            }

            return (await GetByIdAsync(account.Id, cancellationToken))!;
        }

        var perInstallment = Math.Round(account.Amount / installmentCount, 2, MidpointRounding.AwayFromZero);
        var remainder = account.Amount - perInstallment * installmentCount;
        var baseDue = account.DueDate ?? DateTime.UtcNow;
        FinancialAccount? firstAccount = null;

        for (var i = 1; i <= installmentCount; i++)
        {
            var installmentAmount = perInstallment + (i == installmentCount ? remainder : 0m);
            var installment = new FinancialAccount
            {
                Direction = account.Direction,
                PatientId = account.PatientId,
                SupplierId = account.SupplierId,
                CounterpartyName = account.CounterpartyName,
                AppointmentId = account.AppointmentId,
                HospitalizationId = account.HospitalizationId,
                Category = account.Category,
                Description = $"{account.Description} ({i}/{installmentCount})",
                Notes = account.Notes,
                InvoiceNumber = account.InvoiceNumber,
                Amount = installmentAmount,
                DueDate = baseDue.AddMonths(i - 1),
                ExpectedPaymentMethod = account.ExpectedPaymentMethod,
                InstallmentNumber = i,
                InstallmentCount = installmentCount,
                ParentFinancialAccountId = firstAccount?.Id,
            };

            dbContext.FinancialAccounts.Add(installment);
            if (i == 1)
            {
                firstAccount = installment;
                AddLineItems(installment, request.LineItems);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            if (i == 1 && installment.Direction == FinancialAccountDirection.Receivable)
            {
                await ScheduleBillingReminderAsync(installment, cancellationToken);
            }
        }

        return (await GetByIdAsync(firstAccount!.Id, cancellationToken))!;
    }

    private static void AddLineItems(
        FinancialAccount account,
        IReadOnlyList<FinancialAccountLineItemInput>? lineItems)
    {
        if (lineItems is not { Count: > 0 })
        {
            return;
        }

        foreach (var item in lineItems)
        {
            var quantity = Math.Max(1, item.Quantity);
            var unit = item.UnitAmount;
            account.LineItems.Add(new FinancialAccountLineItem
            {
                Description = item.Description.Trim(),
                Quantity = quantity,
                UnitAmount = unit,
                TotalAmount = quantity * unit,
                Notes = string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes.Trim(),
            });
        }
    }

    public async Task<FinancialAccountDto?> RegisterPaymentAsync(
        Guid id,
        RegisterPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await dbContext.FinancialAccounts.FirstOrDefaultAsync(f => f.Id == id && f.IsActive, cancellationToken);
        if (account is null)
        {
            return null;
        }

        if (account.Status is FinancialAccountStatus.Paid or FinancialAccountStatus.Cancelled)
        {
            throw new InvalidOperationException("Conta já quitada ou cancelada.");
        }

        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Valor do pagamento deve ser maior que zero.");
        }

        var balance = account.Amount - account.PaidAmount;
        if (request.Amount > balance)
        {
            throw new InvalidOperationException($"Valor excede o saldo em aberto (R$ {balance:F2}).");
        }

        ValidateInstallments(request);

        var paidAt = request.PaidAt?.ToUniversalTime() ?? DateTime.UtcNow;
        var installmentCount = request.Installments is { Count: > 0 } list ? list.Count : (int?)null;

        var payment = new FinancialPayment
        {
            FinancialAccountId = account.Id,
            Amount = request.Amount,
            Method = request.Method,
            PaidAt = paidAt,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            PixChargeId = request.PixChargeId,
            InstallmentCount = installmentCount
        };

        dbContext.FinancialPayments.Add(payment);

        account.PaidAmount += request.Amount;
        account.PaidAt = paidAt;
        account.UpdatedAt = DateTime.UtcNow;

        if (account.PaidAmount >= account.Amount)
        {
            account.Status = FinancialAccountStatus.Paid;
            account.PaidAmount = account.Amount;
        }
        else
        {
            account.Status = FinancialAccountStatus.PartiallyPaid;
        }

        if (request.Method == PaymentMethod.CreditCard && request.Installments is { Count: > 0 } installments)
        {
            var ordered = installments.OrderBy(i => i.InstallmentNumber).ToList();
            foreach (var installment in ordered)
            {
                var childAccount = new FinancialAccount
                {
                    Direction = account.Direction,
                    PatientId = account.PatientId,
                    SupplierId = account.SupplierId,
                    CounterpartyName = account.CounterpartyName,
                    Category = account.Category,
                    Description =
                        $"Parcela {installment.InstallmentNumber}/{ordered.Count} — Cartão crédito — {account.Description}",
                    Notes = $"Conta gerada pelo parcelamento da conta {account.Id}.",
                    Amount = installment.Amount,
                    DueDate = installment.DueDate.ToUniversalTime(),
                    ExpectedPaymentMethod = PaymentMethod.CreditCard,
                    ParentFinancialAccountId = account.Id,
                    InstallmentNumber = installment.InstallmentNumber,
                    InstallmentCount = ordered.Count
                };

                dbContext.FinancialAccounts.Add(childAccount);

                payment.Installments.Add(new FinancialPaymentInstallment
                {
                    InstallmentNumber = installment.InstallmentNumber,
                    InstallmentCount = ordered.Count,
                    Amount = installment.Amount,
                    DueDate = installment.DueDate.ToUniversalTime(),
                    FinancialAccount = childAccount
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    private static void ValidateInstallments(RegisterPaymentRequest request)
    {
        var installments = request.Installments;
        if (installments is null || installments.Count == 0)
        {
            return;
        }

        if (request.Method != PaymentMethod.CreditCard)
        {
            throw new InvalidOperationException("Parcelamento disponível apenas para cartão de crédito.");
        }

        if (installments.Count < 2)
        {
            throw new InvalidOperationException("Informe ao menos 2 parcelas.");
        }

        if (installments.Select(i => i.InstallmentNumber).Distinct().Count() != installments.Count)
        {
            throw new InvalidOperationException("Número de parcela duplicado.");
        }

        var total = installments.Sum(i => i.Amount);
        if (total != request.Amount)
        {
            throw new InvalidOperationException(
                $"A soma das parcelas (R$ {total:F2}) deve ser igual ao valor do pagamento (R$ {request.Amount:F2}).");
        }

        foreach (var installment in installments)
        {
            if (installment.InstallmentNumber <= 0)
            {
                throw new InvalidOperationException("Número da parcela inválido.");
            }

            if (installment.Amount <= 0)
            {
                throw new InvalidOperationException("Cada parcela deve ter valor maior que zero.");
            }
        }
    }

    public async Task<IReadOnlyList<FinancialPaymentDto>> GetPaymentsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.FinancialPayments
            .AsNoTracking()
            .Where(p => p.FinancialAccountId == accountId && p.IsActive)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new FinancialPaymentDto(
                p.Id,
                p.FinancialAccountId,
                p.Amount,
                p.Method,
                p.PaidAt,
                p.Notes,
                p.CreatedAt,
                p.InstallmentCount,
                p.Installments
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.InstallmentNumber)
                    .Select(i => new FinancialPaymentInstallmentDto(
                        i.InstallmentNumber,
                        i.InstallmentCount,
                        i.Amount,
                        i.DueDate,
                        i.FinancialAccountId))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<FinancialSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var receivableOpen = await dbContext.FinancialAccounts
            .Where(f => f.IsActive
                && f.Direction == FinancialAccountDirection.Receivable
                && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid))
            .SumAsync(f => f.Amount - f.PaidAmount, cancellationToken);

        var payableOpen = await dbContext.FinancialAccounts
            .Where(f => f.IsActive
                && f.Direction == FinancialAccountDirection.Payable
                && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid))
            .SumAsync(f => f.Amount - f.PaidAmount, cancellationToken);

        var paymentQuery = dbContext.FinancialPayments
            .AsNoTracking()
            .Where(p => p.IsActive && p.FinancialAccount.IsActive);

        var totalReceived = await paymentQuery
            .Where(p => p.FinancialAccount.Direction == FinancialAccountDirection.Receivable)
            .SumAsync(p => p.Amount, cancellationToken);

        var totalPaidOut = await paymentQuery
            .Where(p => p.FinancialAccount.Direction == FinancialAccountDirection.Payable)
            .SumAsync(p => p.Amount, cancellationToken);

        var receivedThisMonth = await paymentQuery
            .Where(p => p.FinancialAccount.Direction == FinancialAccountDirection.Receivable && p.PaidAt >= startOfMonth)
            .SumAsync(p => p.Amount, cancellationToken);

        var paidOutThisMonth = await paymentQuery
            .Where(p => p.FinancialAccount.Direction == FinancialAccountDirection.Payable && p.PaidAt >= startOfMonth)
            .SumAsync(p => p.Amount, cancellationToken);

        var openReceivables = dbContext.FinancialAccounts
            .AsNoTracking()
            .Where(f => f.IsActive
                && f.Direction == FinancialAccountDirection.Receivable
                && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid));

        var openProposals = openReceivables
            .Where(f => EF.Functions.ILike(f.Description, "%proposta%"));

        var openHonorarios = openReceivables
            .Where(f => EF.Functions.ILike(f.Description, "%honor%"));

        var openProposalsCount = await openProposals.CountAsync(cancellationToken);
        var openProposalsBalance = await openProposals.SumAsync(f => f.Amount - f.PaidAmount, cancellationToken);
        var openHonorariosCount = await openHonorarios.CountAsync(cancellationToken);
        var openHonorariosBalance = await openHonorarios.SumAsync(f => f.Amount - f.PaidAmount, cancellationToken);

        return new FinancialSummaryDto(
            receivableOpen,
            payableOpen,
            totalReceived,
            totalPaidOut,
            receivedThisMonth,
            paidOutThisMonth,
            openProposalsCount,
            openProposalsBalance,
            openHonorariosCount,
            openHonorariosBalance);
    }

    public async Task<FinancialAccountDto?> ConvertProposalToBillingAsync(
        Guid proposalId,
        CancellationToken cancellationToken = default)
    {
        var proposal = await dbContext.FinancialAccounts
            .Include(f => f.LineItems.Where(li => li.IsActive))
            .FirstOrDefaultAsync(f => f.Id == proposalId && f.IsActive, cancellationToken);

        if (proposal is null)
        {
            return null;
        }

        if (proposal.Direction != FinancialAccountDirection.Receivable)
        {
            throw new InvalidOperationException("Somente propostas a receber podem ser convertidas.");
        }

        if (!proposal.Description.Contains("proposta", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Lançamento não identificado como proposta.");
        }

        var alreadyConverted = await dbContext.FinancialAccounts.AnyAsync(
            f => f.ParentFinancialAccountId == proposalId
                && f.IsActive
                && !EF.Functions.ILike(f.Description, "%proposta%"),
            cancellationToken);

        if (alreadyConverted)
        {
            throw new InvalidOperationException("Proposta já convertida em faturamento.");
        }

        var outstanding = proposal.Amount - proposal.PaidAmount;
        if (outstanding <= 0)
        {
            throw new InvalidOperationException("Proposta sem saldo em aberto para faturamento.");
        }

        var billingDescription = proposal.Description
            .Replace("Proposta —", "Faturamento —", StringComparison.OrdinalIgnoreCase)
            .Replace("Proposta -", "Faturamento -", StringComparison.OrdinalIgnoreCase);

        if (billingDescription.Equals(proposal.Description, StringComparison.OrdinalIgnoreCase))
        {
            billingDescription = $"Faturamento — {proposal.Description.Trim()}";
        }

        var billing = new FinancialAccount
        {
            Direction = FinancialAccountDirection.Receivable,
            PatientId = proposal.PatientId,
            AppointmentId = proposal.AppointmentId,
            HospitalizationId = proposal.HospitalizationId,
            HealthInsuranceId = proposal.HealthInsuranceId,
            Category = proposal.Category,
            Description = billingDescription.Trim(),
            Notes = $"Convertido da proposta {proposal.Id:N}",
            InvoiceNumber = proposal.InvoiceNumber,
            Amount = outstanding,
            DueDate = proposal.DueDate ?? DateTime.UtcNow.AddDays(15),
            ExpectedPaymentMethod = proposal.ExpectedPaymentMethod,
            ParentFinancialAccountId = proposal.Id,
        };

        dbContext.FinancialAccounts.Add(billing);

        foreach (var item in proposal.LineItems.Where(li => li.IsActive))
        {
            billing.LineItems.Add(new FinancialAccountLineItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitAmount = item.UnitAmount,
                TotalAmount = item.TotalAmount > 0 ? item.TotalAmount : item.Quantity * item.UnitAmount,
                Notes = item.Notes,
            });
        }

        proposal.Notes = string.IsNullOrWhiteSpace(proposal.Notes)
            ? $"Convertida em faturamento em {DateTime.UtcNow:yyyy-MM-dd}"
            : $"{proposal.Notes.Trim()} | Convertida em faturamento em {DateTime.UtcNow:yyyy-MM-dd}";

        await dbContext.SaveChangesAsync(cancellationToken);
        await ScheduleBillingReminderAsync(billing, cancellationToken);

        return await GetByIdAsync(billing.Id, cancellationToken);
    }

    public async Task CreateFromAppointmentAsync(
        Guid appointmentId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.FinancialAccounts.AnyAsync(
            f => f.AppointmentId == appointmentId && f.IsActive,
            cancellationToken);

        if (exists)
        {
            return;
        }

        var appointment = await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Professional).ThenInclude(p => p.Specialty)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return;
        }

        var description = $"Consulta — {appointment.Professional.Specialty.Name} — {appointment.ScheduledAt:dd/MM/yyyy HH:mm}";

        var account = new FinancialAccount
        {
            Direction = FinancialAccountDirection.Receivable,
            PatientId = appointment.PatientId,
            AppointmentId = appointmentId,
            Category = FinancialAccountCategory.Consultation,
            Description = description,
            Amount = amount,
            DueDate = DateTime.UtcNow.AddDays(30)
        };

        dbContext.FinancialAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        await ScheduleBillingReminderAsync(account, cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialAccountDto>> GetOutstandingByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.FinancialAccounts
            .AsNoTracking()
            .Where(f => f.PatientId == patientId && f.IsActive && f.Direction == FinancialAccountDirection.Receivable
                && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid))
            .OrderBy(f => f.DueDate)
            .ThenByDescending(f => f.CreatedAt)
            .Select(MapToDto())
            .ToListAsync(cancellationToken);
    }

    private async Task ScheduleBillingReminderAsync(FinancialAccount account, CancellationToken cancellationToken)
    {
        if (account.Direction != FinancialAccountDirection.Receivable || !account.PatientId.HasValue || !account.DueDate.HasValue)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(new { financialAccountId = account.Id });
        var exists = await dbContext.ConnectScheduledMessages.AnyAsync(
            m => m.IsActive && !m.IsSent
                && m.ReminderType == ConnectReminderType.BillingReminder
                && m.PayloadJson == payload,
            cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.ConnectScheduledMessages.Add(new ConnectScheduledMessage
        {
            PatientId = account.PatientId!.Value,
            ReminderType = ConnectReminderType.BillingReminder,
            ScheduledFor = account.DueDate.Value,
            PayloadJson = payload,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<FinancialAccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.FinancialAccounts
            .AsNoTracking()
            .Where(f => f.Id == id)
            .Select(MapToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<FinancialAccount, FinancialAccountDto>> MapToDto() =>
        f => new FinancialAccountDto(
            f.Id,
            f.Direction,
            f.PatientId,
            f.Patient != null ? f.Patient.FullName : null,
            f.SupplierId,
            f.Supplier != null ? f.Supplier.Name : null,
            f.CounterpartyName,
            f.Patient != null
                ? f.Patient.FullName
                : (f.Supplier != null
                    ? f.Supplier.Name
                    : (f.CounterpartyName ?? "—")),
            f.AppointmentId,
            f.HospitalizationId,
            f.Category,
            f.Description,
            f.Notes,
            f.InvoiceNumber,
            f.Amount,
            f.PaidAmount,
            f.Amount - f.PaidAmount,
            f.Status,
            f.DueDate,
            f.Payments.Where(p => p.IsActive).OrderByDescending(p => p.PaidAt).Select(p => (DateTime?)p.PaidAt).FirstOrDefault()
                ?? f.PaidAt,
            f.Payments.Where(p => p.IsActive).OrderByDescending(p => p.PaidAt).Select(p => (PaymentMethod?)p.Method).FirstOrDefault(),
            f.ExpectedPaymentMethod,
            f.Payments.Count(p => p.IsActive),
            f.CreatedAt,
            f.ParentFinancialAccountId,
            f.InstallmentNumber,
            f.InstallmentCount,
            f.LineItems
                .Where(li => li.IsActive)
                .OrderBy(li => li.CreatedAt)
                .Select(li => new FinancialAccountLineItemDto(
                    li.Id,
                    li.Description,
                    li.Quantity,
                    li.UnitAmount,
                    li.TotalAmount,
                    li.Notes))
                .ToList());

    private static IReadOnlyList<FinancialAccountCategoryPresetDto> BuildCategoryPresets(
        int modality,
        string? insuranceName)
    {
        var consultationAmount = modality == 1 ? ConsultationParticularAmount : ConsultationCopaymentAmount;
        var insuranceSuffix = string.IsNullOrWhiteSpace(insuranceName) ? "Particular" : insuranceName;

        return
        [
            new(FinancialAccountCategory.Consultation, "Consulta ambulatorial", consultationAmount,
                $"Consulta ambulatorial — {insuranceSuffix}", SuggestedDueDays(modality, FinancialAccountCategory.Consultation)),
            new(FinancialAccountCategory.Hospitalization, "Internação / diárias", 450m,
                $"Internação — diárias e taxas ({insuranceSuffix})", SuggestedDueDays(modality, FinancialAccountCategory.Hospitalization)),
            new(FinancialAccountCategory.Exam, "Exames laboratoriais", ExamPackageAmount,
                "Pacote exames laboratoriais ambulatoriais", SuggestedDueDays(modality, FinancialAccountCategory.Exam)),
            new(FinancialAccountCategory.Copayment, "Coparticipação convênio", DefaultCopaymentAmount,
                $"Coparticipação — {insuranceSuffix}", SuggestedDueDays(modality, FinancialAccountCategory.Copayment)),
            new(FinancialAccountCategory.Parking, "Estacionamento", 24m,
                "Estacionamento — permanência", 3),
            new(FinancialAccountCategory.Other, "Outros serviços", 0m,
                "Serviço hospitalar", SuggestedDueDays(modality, FinancialAccountCategory.Other)),
        ];
    }

    private static int ResolvePaymentModality(string? insuranceName)
    {
        if (string.IsNullOrWhiteSpace(insuranceName))
        {
            return 1;
        }

        var normalized = insuranceName.Trim().ToLowerInvariant();
        if (normalized is "sus" or "sistema único de saúde")
        {
            return 3;
        }

        if (normalized is "particular" or "privado")
        {
            return 1;
        }

        return 2;
    }

    private static int SuggestedDueDays(int modality, FinancialAccountCategory category = FinancialAccountCategory.Other)
    {
        if (category == FinancialAccountCategory.Parking)
        {
            return 3;
        }

        return modality switch
        {
            1 => 7,
            2 => category == FinancialAccountCategory.Copayment ? 15 : 10,
            3 => 30,
            _ => 15
        };
    }

    private static decimal DailyRateForWard(WardCategory category) => category switch
    {
        WardCategory.Uti => 1850m,
        WardCategory.Apartamento => 680m,
        WardCategory.Pediatrica => 520m,
        WardCategory.Maternidade => 590m,
        _ => 450m
    };
}
