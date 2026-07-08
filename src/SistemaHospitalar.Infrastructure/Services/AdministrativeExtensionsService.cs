using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Administrative;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Services.Payroll;

namespace SistemaHospitalar.Infrastructure.Services;

public class AdministrativeExtensionsService(AppDbContext dbContext) : IAdministrativeExtensionsService
{
    public async Task<IReadOnlyList<TpaAdministratorDto>> GetTpaAdministratorsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.TpaAdministrators
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new TpaAdministratorDto(
                x.Id,
                x.Name,
                x.Cnpj,
                x.ContactName,
                x.ContactEmail,
                x.CommissionPercent,
                x.DiscountPercent,
                x.Claims.Count(c => c.IsActive)))
            .ToListAsync(cancellationToken);
    }

    public async Task<TpaAdministratorDto> CreateTpaAdministratorAsync(CreateTpaAdministratorRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new TpaAdministrator
        {
            Name = request.Name.Trim(),
            Cnpj = request.Cnpj?.Trim(),
            ContactName = request.ContactName?.Trim(),
            ContactEmail = request.ContactEmail?.Trim(),
            CommissionPercent = request.CommissionPercent,
            DiscountPercent = request.DiscountPercent
        };

        dbContext.TpaAdministrators.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TpaAdministratorDto(
            entity.Id,
            entity.Name,
            entity.Cnpj,
            entity.ContactName,
            entity.ContactEmail,
            entity.CommissionPercent,
            entity.DiscountPercent,
            0);
    }

    public async Task<IReadOnlyList<TpaClaimDto>> GetTpaClaimsAsync(
        Guid? administratorId = null,
        TpaClaimStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.TpaClaims.AsNoTracking().Where(x => x.IsActive);
        if (administratorId.HasValue) query = query.Where(x => x.TpaAdministratorId == administratorId.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);

        return await query
            .OrderByDescending(x => x.ServiceDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(MapTpaClaimDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<TpaClaimDto> CreateTpaClaimAsync(CreateTpaClaimRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await dbContext.TpaAdministrators
            .FirstOrDefaultAsync(x => x.Id == request.TpaAdministratorId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Administradora TPA não encontrada.");

        var patientExists = await dbContext.Patients.AnyAsync(x => x.Id == request.PatientId && x.IsActive, cancellationToken);
        if (!patientExists) throw new InvalidOperationException("Paciente não encontrado.");

        if (request.HealthInsuranceId.HasValue)
        {
            var insuranceExists = await dbContext.HealthInsurances.AnyAsync(x => x.Id == request.HealthInsuranceId.Value && x.IsActive, cancellationToken);
            if (!insuranceExists) throw new InvalidOperationException("Convênio informado não encontrado.");
        }

        var gross = request.GrossAmount;
        var commissionPercent = request.CommissionPercent ?? admin.CommissionPercent;
        var discountPercent = request.DiscountPercent ?? admin.DiscountPercent;
        var commission = Math.Round(gross * commissionPercent / 100m, 2);
        var discount = Math.Round(gross * discountPercent / 100m, 2);
        var net = gross - commission - discount;

        var entity = new TpaClaim
        {
            TpaAdministratorId = request.TpaAdministratorId,
            PatientId = request.PatientId,
            HealthInsuranceId = request.HealthInsuranceId,
            ServiceDate = request.ServiceDate,
            GrossAmount = gross,
            CommissionAmount = commission,
            DiscountAmount = discount,
            NetAmount = net,
            Notes = request.Notes?.Trim()
        };

        dbContext.TpaClaims.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await dbContext.TpaClaims.AsNoTracking().Where(x => x.Id == entity.Id).Select(MapTpaClaimDto()).FirstAsync(cancellationToken));
    }

    public async Task<TpaClaimDto?> UpdateTpaClaimStatusAsync(Guid claimId, UpdateTpaClaimStatusRequest request, CancellationToken cancellationToken = default)
    {
        var claim = await dbContext.TpaClaims.FirstOrDefaultAsync(x => x.Id == claimId && x.IsActive, cancellationToken);
        if (claim is null) return null;

        claim.Status = request.Status;
        claim.UpdatedAt = DateTime.UtcNow;

        if (request.Status == TpaClaimStatus.Paid && request.CreateFinancialAccountWhenPaid && claim.FinancialAccountId is null)
        {
            var account = new FinancialAccount
            {
                Direction = FinancialAccountDirection.Receivable,
                PatientId = claim.PatientId,
                HealthInsuranceId = claim.HealthInsuranceId,
                Category = FinancialAccountCategory.InsuranceReceivable,
                Description = $"TPA {claim.ServiceDate:MM/yyyy}",
                Amount = claim.NetAmount,
                PaidAmount = claim.NetAmount,
                Status = FinancialAccountStatus.Paid,
                PaidAt = DateTime.UtcNow,
                DueDate = claim.ServiceDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(30),
                Notes = "Conta gerada automaticamente a partir de claim TPA."
            };
            dbContext.FinancialAccounts.Add(account);
            await dbContext.SaveChangesAsync(cancellationToken);
            claim.FinancialAccountId = account.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.TpaClaims.AsNoTracking().Where(x => x.Id == claimId).Select(MapTpaClaimDto()).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TpaReportDto> GetTpaReportAsync(CancellationToken cancellationToken = default)
    {
        var query = dbContext.TpaClaims.AsNoTracking().Where(x => x.IsActive);
        var totalClaims = await query.CountAsync(cancellationToken);
        var grossTotal = await query.SumAsync(x => x.GrossAmount, cancellationToken);
        var netTotal = await query.SumAsync(x => x.NetAmount, cancellationToken);
        var latest = await query.OrderByDescending(x => x.ServiceDate).Take(20).Select(MapTpaClaimDto()).ToListAsync(cancellationToken);

        return new TpaReportDto(totalClaims, grossTotal, netTotal, latest);
    }

    public async Task<IReadOnlyList<PayrollRunDto>> GetPayrollRunsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.PayrollRuns
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .Select(MapPayrollRunDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<PayrollRunDto?> GetPayrollRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PayrollRuns
            .AsNoTracking()
            .Where(x => x.Id == runId && x.IsActive)
            .Select(MapPayrollRunDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PayrollRunDto> GeneratePayrollRunAsync(GeneratePayrollRunRequest request, CancellationToken cancellationToken = default)
    {
        var periodEnd = new DateOnly(request.Year, request.Month, DateTime.DaysInMonth(request.Year, request.Month));
        var periodStart = new DateOnly(request.Year, request.Month, 1);

        var employees = await dbContext.Employees
            .AsNoTracking()
            .Where(x => x.IsActive && x.HireDate <= periodEnd)
            .ToListAsync(cancellationToken);

        if (employees.Count == 0) throw new InvalidOperationException("Nenhum colaborador elegível para gerar a folha.");

        var existing = await dbContext.PayrollRuns
            .FirstOrDefaultAsync(x => x.IsActive && x.Year == request.Year && x.Month == request.Month, cancellationToken);
        if (existing is not null) throw new InvalidOperationException("Já existe folha para o período informado.");

        var employeeIds = employees.Select(e => e.Id).ToList();
        var shiftStats = await dbContext.EmployeeShifts
            .AsNoTracking()
            .Where(s => s.IsActive
                && employeeIds.Contains(s.EmployeeId)
                && s.ShiftDate >= periodStart
                && s.ShiftDate <= periodEnd)
            .GroupBy(s => s.EmployeeId)
            .Select(g => new
            {
                EmployeeId = g.Key,
                TotalShifts = g.Count(),
                NightShifts = g.Count(s => s.ShiftType == ShiftType.Night),
            })
            .ToDictionaryAsync(x => x.EmployeeId, cancellationToken);

        var run = new PayrollRun
        {
            Year = request.Year,
            Month = request.Month,
            ReferenceDate = periodStart,
            Status = PayrollRunStatus.Generated,
            GeneratedAt = DateTime.UtcNow,
            Notes = request.Notes?.Trim()
        };

        foreach (var employee in employees)
        {
            shiftStats.TryGetValue(employee.Id, out var shifts);
            var baseSalary = employee.BaseSalary > 0
                ? employee.BaseSalary
                : request.DefaultBaseSalary is > 0
                    ? request.DefaultBaseSalary.Value
                    : PayrollCalculationService.DefaultSalaryForRole(employee.Role);

            var expectedShifts = PayrollCalculationService.ExpectedShiftsForRole(employee.Role);
            var totalShifts = shifts?.TotalShifts ?? 0;
            var absenceDays = Math.Max(0, expectedShifts - totalShifts);

            var calc = PayrollCalculationService.Calculate(new PayrollCalculationService.PayrollCalculationInput(
                employee.Role,
                baseSalary,
                request.Year,
                request.Month,
                shifts?.NightShifts ?? 0,
                totalShifts,
                absenceDays,
                request.ValeRefeicao,
                request.ValeTransportePercent,
                request.HealthPlanDiscount,
                request.DependentCount));

            run.Items.Add(PayrollCalculationService.BuildPayrollItem(employee.Id, calc));
        }

        run.TotalGross = run.Items.Sum(x => x.GrossAmount);
        run.TotalDiscounts = run.Items.Sum(x => x.DiscountAmount);
        run.TotalNet = run.Items.Sum(x => x.NetAmount);
        run.TotalFgtsEmployer = run.Items.Sum(x => x.FgtsEmployerAmount);

        dbContext.PayrollRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await dbContext.PayrollRuns.AsNoTracking().Where(x => x.Id == run.Id).Select(MapPayrollRunDto()).FirstAsync(cancellationToken));
    }

    public async Task<PayrollRunDto?> UpdatePayrollRunStatusAsync(Guid runId, UpdatePayrollRunStatusRequest request, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.PayrollRuns
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == runId && x.IsActive, cancellationToken);
        if (run is null) return null;

        run.Status = request.Status;
        run.UpdatedAt = DateTime.UtcNow;
        if (request.Status == PayrollRunStatus.Approved) run.ApprovedAt = DateTime.UtcNow;
        if (request.Status == PayrollRunStatus.Paid)
        {
            run.PaidAt = DateTime.UtcNow;
            if (request.CreateFinancialAccountsWhenPaid && run.ConsolidatedFinancialAccountId is null)
            {
                var account = new FinancialAccount
                {
                    Direction = FinancialAccountDirection.Payable,
                    CounterpartyName = "Folha de pagamento",
                    Category = FinancialAccountCategory.Payroll,
                    Description = $"Folha pagamento — {run.Month:D2}/{run.Year}",
                    Amount = run.TotalNet,
                    PaidAmount = run.TotalNet,
                    Status = FinancialAccountStatus.Paid,
                    PaidAt = DateTime.UtcNow,
                    DueDate = new DateTime(run.Year, run.Month, DateTime.DaysInMonth(run.Year, run.Month), 0, 0, 0, DateTimeKind.Utc),
                    Notes = $"Conta consolidada da folha com {run.Items.Count} colaborador(es)."
                };
                dbContext.FinancialAccounts.Add(account);
                await dbContext.SaveChangesAsync(cancellationToken);
                run.ConsolidatedFinancialAccountId = account.Id;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await dbContext.PayrollRuns.AsNoTracking().Where(x => x.Id == run.Id).Select(MapPayrollRunDto()).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PayrollItemDto?> UpdatePayrollItemLinesAsync(
        Guid runId,
        Guid itemId,
        UpdatePayrollItemLinesRequest request,
        CancellationToken cancellationToken = default)
    {
        var run = await dbContext.PayrollRuns
            .Include(x => x.Items)
            .ThenInclude(x => x.Lines)
            .Include(x => x.Items)
            .ThenInclude(x => x.Employee)
            .ThenInclude(x => x.Department)
            .FirstOrDefaultAsync(x => x.Id == runId && x.IsActive, cancellationToken);
        if (run is null)
        {
            return null;
        }

        if (run.Status is PayrollRunStatus.Approved or PayrollRunStatus.Paid)
        {
            throw new InvalidOperationException("Folha fechada — não é possível alterar proventos/descontos.");
        }

        var item = run.Items.FirstOrDefault(x => x.Id == itemId && x.IsActive);
        if (item is null)
        {
            return null;
        }

        if (request.Lines.Count == 0)
        {
            throw new InvalidOperationException("Informe ao menos uma linha de provento ou desconto.");
        }

        foreach (var line in item.Lines)
        {
            line.IsActive = false;
            line.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var input in request.Lines)
        {
            var code = input.Code.Trim();
            var description = input.Description.Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(description) || input.Amount <= 0)
            {
                continue;
            }

            item.Lines.Add(new PayrollItemLine
            {
                LineType = input.LineType,
                Code = code,
                Description = description,
                Amount = Math.Round(input.Amount, 2),
            });
        }

        PayrollCalculationService.RecalculateItemTotalsFromLines(item);
        item.UpdatedAt = DateTime.UtcNow;

        run.TotalGross = run.Items.Where(i => i.IsActive).Sum(i => i.GrossAmount);
        run.TotalDiscounts = run.Items.Where(i => i.IsActive).Sum(i => i.DiscountAmount);
        run.TotalNet = run.Items.Where(i => i.IsActive).Sum(i => i.NetAmount);
        run.TotalFgtsEmployer = run.Items.Where(i => i.IsActive).Sum(i => i.FgtsEmployerAmount);
        run.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.PayrollItems
            .AsNoTracking()
            .Where(x => x.Id == itemId && x.IsActive)
            .Select(x => new PayrollItemDto(
                x.Id,
                x.EmployeeId,
                x.Employee.FullName,
                x.Employee.JobTitle,
                x.Employee.Department.Name,
                x.BaseSalary,
                x.OvertimeAmount,
                x.BenefitsAmount,
                x.DiscountAmount,
                x.GrossAmount,
                x.NetAmount,
                x.FgtsEmployerAmount,
                x.FinancialAccountId,
                x.Lines
                    .Where(l => l.IsActive)
                    .OrderBy(l => l.LineType)
                    .ThenBy(l => l.Code)
                    .Select(l => new PayrollItemLineDto(l.Id, l.LineType, l.Code, l.Description, l.Amount))
                    .ToList()))
            .FirstAsync(cancellationToken);
    }

    public async Task<PayrollSlipDto?> GetPayrollSlipAsync(Guid runId, Guid employeeId, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.PayrollRuns
            .AsNoTracking()
            .Where(x => x.Id == runId && x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.Year,
                x.Month,
                x.ReferenceDate,
                x.Status
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (run is null) return null;

        var item = await dbContext.PayrollItems
            .AsNoTracking()
            .Where(x => x.PayrollRunId == runId && x.EmployeeId == employeeId && x.IsActive)
            .Select(x => new PayrollItemDto(
                x.Id,
                x.EmployeeId,
                x.Employee.FullName,
                x.Employee.JobTitle,
                x.Employee.Department.Name,
                x.BaseSalary,
                x.OvertimeAmount,
                x.BenefitsAmount,
                x.DiscountAmount,
                x.GrossAmount,
                x.NetAmount,
                x.FgtsEmployerAmount,
                x.FinancialAccountId,
                x.Lines
                    .Where(l => l.IsActive)
                    .OrderBy(l => l.LineType)
                    .ThenBy(l => l.Code)
                    .Select(l => new PayrollItemLineDto(l.Id, l.LineType, l.Code, l.Description, l.Amount))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null) return null;

        var earnings = item.Lines.Where(l => l.LineType == PayrollLineType.Earning).ToList();
        var discounts = item.Lines.Where(l => l.LineType == PayrollLineType.Discount).ToList();

        return new PayrollSlipDto(
            run.Id,
            run.Year,
            run.Month,
            run.ReferenceDate,
            run.Status,
            item,
            item.FgtsEmployerAmount,
            earnings,
            discounts);
    }

    public async Task<PayrollMonthlySummaryDto> GetPayrollMonthlySummaryAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var run = await dbContext.PayrollRuns
            .AsNoTracking()
            .Where(r => r.IsActive && r.Year == year && r.Month == month)
            .Select(r => new
            {
                r.Id,
                r.Status,
                r.TotalGross,
                r.TotalDiscounts,
                r.TotalNet,
                r.TotalFgtsEmployer,
                Items = r.Items
                    .Where(i => i.IsActive)
                    .Select(i => new
                    {
                        i.GrossAmount,
                        i.NetAmount,
                        Department = i.Employee.Department.Name,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var employeesOnVacation = await dbContext.EmployeeHrEvents
            .CountAsync(e => e.IsActive
                && e.EventType == HrEventType.Vacation
                && e.StartDate <= monthEnd
                && (e.EndDate == null || e.EndDate >= monthStart), cancellationToken);

        var nightShiftsInMonth = await dbContext.EmployeeShifts
            .CountAsync(s => s.IsActive
                && s.ShiftDate >= monthStart
                && s.ShiftDate <= monthEnd
                && s.ShiftType == ShiftType.Night, cancellationToken);

        if (run is null)
        {
            return new PayrollMonthlySummaryDto(
                year,
                month,
                null,
                null,
                0,
                0m,
                0m,
                0m,
                0m,
                employeesOnVacation,
                nightShiftsInMonth,
                Array.Empty<PayrollDepartmentSummaryDto>());
        }

        var byDepartment = run.Items
            .GroupBy(i => i.Department)
            .OrderBy(g => g.Key)
            .Select(g => new PayrollDepartmentSummaryDto(
                g.Key,
                g.Count(),
                g.Sum(i => i.GrossAmount),
                g.Sum(i => i.NetAmount)))
            .ToList();

        return new PayrollMonthlySummaryDto(
            year,
            month,
            run.Status,
            run.Id,
            run.Items.Count,
            run.TotalGross,
            run.TotalDiscounts,
            run.TotalNet,
            run.TotalFgtsEmployer,
            employeesOnVacation,
            nightShiftsInMonth,
            byDepartment);
    }

    public async Task<IReadOnlyList<PharmacyBillingEntryDto>> GetPharmacyBillingEntriesAsync(bool? paid = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.PharmacyBillingEntries.AsNoTracking().Where(x => x.IsActive);
        if (paid.HasValue) query = query.Where(x => x.Paid == paid.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PharmacyBillingEntryDto(
                x.Id,
                x.DispensingId,
                x.Dispensing.DispensedAt,
                x.Dispensing.Patient.FullName,
                x.Dispensing.Product.Name,
                x.Dispensing.Quantity,
                x.PayerType,
                x.HealthInsuranceId,
                x.HealthInsurance != null ? x.HealthInsurance.Name : null,
                x.UnitPrice,
                x.TotalAmount,
                x.Paid,
                x.PaidAt,
                x.FinancialAccountId,
                x.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<PharmacyBillingEntryDto> CreatePharmacyBillingEntryAsync(CreatePharmacyBillingEntryRequest request, CancellationToken cancellationToken = default)
    {
        var dispensing = await dbContext.PharmacyDispensings
            .Include(x => x.Patient)
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == request.DispensingId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Dispensação não encontrada.");

        if (request.PayerType == PharmacyBillingPayerType.Insurance && request.HealthInsuranceId is null)
        {
            throw new InvalidOperationException("Informe o convênio para faturamento por convênio.");
        }

        var total = Math.Round(request.UnitPrice * dispensing.Quantity, 2);
        var entity = new PharmacyBillingEntry
        {
            DispensingId = request.DispensingId,
            PayerType = request.PayerType,
            HealthInsuranceId = request.HealthInsuranceId,
            UnitPrice = request.UnitPrice,
            TotalAmount = total,
            Paid = request.Paid,
            PaidAt = request.Paid ? DateTime.UtcNow : null,
            Notes = request.Notes?.Trim()
        };

        dbContext.PharmacyBillingEntries.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.Paid && request.CreateFinancialAccountWhenPaid)
        {
            var account = new FinancialAccount
            {
                Direction = FinancialAccountDirection.Receivable,
                PatientId = dispensing.PatientId,
                HealthInsuranceId = request.HealthInsuranceId,
                Category = FinancialAccountCategory.Exam,
                Description = $"Venda farmácia - {dispensing.Product.Name}",
                Amount = total,
                PaidAmount = total,
                Status = FinancialAccountStatus.Paid,
                PaidAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow,
                Notes = "Conta criada a partir de faturamento da farmácia."
            };
            dbContext.FinancialAccounts.Add(account);
            await dbContext.SaveChangesAsync(cancellationToken);
            entity.FinancialAccountId = account.Id;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await dbContext.PharmacyBillingEntries.AsNoTracking()
            .Where(x => x.Id == entity.Id)
            .Select(x => new PharmacyBillingEntryDto(
                x.Id,
                x.DispensingId,
                x.Dispensing.DispensedAt,
                x.Dispensing.Patient.FullName,
                x.Dispensing.Product.Name,
                x.Dispensing.Quantity,
                x.PayerType,
                x.HealthInsuranceId,
                x.HealthInsurance != null ? x.HealthInsurance.Name : null,
                x.UnitPrice,
                x.TotalAmount,
                x.Paid,
                x.PaidAt,
                x.FinancialAccountId,
                x.Notes))
            .FirstAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BirthRegistrationDto>> GetBirthRegistrationsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.BirthRegistrations
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.BirthAt)
            .Take(200)
            .Select(x => new BirthRegistrationDto(
                x.Id,
                x.MotherPatientId,
                x.MotherPatient.FullName,
                x.BabyName,
                x.BirthAt,
                x.WeightKg,
                x.HeightCm,
                x.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<BirthRegistrationDto> CreateBirthRegistrationAsync(CreateBirthRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var motherExists = await dbContext.Patients.AnyAsync(x => x.Id == request.MotherPatientId && x.IsActive, cancellationToken);
        if (!motherExists) throw new InvalidOperationException("Mãe não encontrada.");

        var entity = new BirthRegistration
        {
            MotherPatientId = request.MotherPatientId,
            BabyName = request.BabyName.Trim(),
            BirthAt = request.BirthAt,
            WeightKg = request.WeightKg,
            HeightCm = request.HeightCm,
            Notes = request.Notes?.Trim()
        };
        dbContext.BirthRegistrations.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.BirthRegistrations.AsNoTracking()
            .Where(x => x.Id == entity.Id)
            .Select(x => new BirthRegistrationDto(
                x.Id,
                x.MotherPatientId,
                x.MotherPatient.FullName,
                x.BabyName,
                x.BirthAt,
                x.WeightKg,
                x.HeightCm,
                x.Notes))
            .FirstAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<TpaClaim, TpaClaimDto>> MapTpaClaimDto() =>
        x => new TpaClaimDto(
            x.Id,
            x.TpaAdministratorId,
            x.TpaAdministrator.Name,
            x.PatientId,
            x.Patient.FullName,
            x.HealthInsuranceId,
            x.HealthInsurance != null ? x.HealthInsurance.Name : null,
            x.ServiceDate,
            x.GrossAmount,
            x.CommissionAmount,
            x.DiscountAmount,
            x.NetAmount,
            x.Status,
            x.Notes,
            x.FinancialAccountId);

    private static System.Linq.Expressions.Expression<Func<PayrollRun, PayrollRunDto>> MapPayrollRunDto() =>
        x => new PayrollRunDto(
            x.Id,
            x.Year,
            x.Month,
            x.ReferenceDate,
            x.Status,
            x.TotalGross,
            x.TotalDiscounts,
            x.TotalNet,
            x.TotalFgtsEmployer,
            x.GeneratedAt,
            x.ApprovedAt,
            x.PaidAt,
            x.Notes,
            x.ConsolidatedFinancialAccountId,
            x.Items
                .Where(i => i.IsActive)
                .OrderBy(i => i.Employee.FullName)
                .Select(i => new PayrollItemDto(
                    i.Id,
                    i.EmployeeId,
                    i.Employee.FullName,
                    i.Employee.JobTitle,
                    i.Employee.Department.Name,
                    i.BaseSalary,
                    i.OvertimeAmount,
                    i.BenefitsAmount,
                    i.DiscountAmount,
                    i.GrossAmount,
                    i.NetAmount,
                    i.FgtsEmployerAmount,
                    i.FinancialAccountId,
                    i.Lines
                        .Where(l => l.IsActive)
                        .OrderBy(l => l.LineType)
                        .ThenBy(l => l.Code)
                        .Select(l => new PayrollItemLineDto(l.Id, l.LineType, l.Code, l.Description, l.Amount))
                        .ToList()))
                .ToList());
}

