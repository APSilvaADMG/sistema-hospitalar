using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Services.Payroll;

public static class PayrollCalculationService
{
    private const decimal MinimumWage = 1412m;
    private const decimal MonthlyWorkHours = 220m;
    private const decimal HoursPerShift = 8m;
    private const decimal InssCeiling = 7786.02m;
    private const decimal IrrfDependentDeduction = 189.59m;

    public record PayrollCalculationInput(
        EmployeeRole Role,
        decimal BaseSalary,
        int Year,
        int Month,
        int NightShiftCount,
        int TotalShiftCount,
        int AbsenceDays,
        decimal ValeRefeicao,
        decimal ValeTransportePercent,
        decimal HealthPlanDiscount,
        int DependentCount,
        decimal BonusAmount = 0m);

    public record PayrollLineResult(PayrollLineType LineType, string Code, string Description, decimal Amount);

    public record PayrollCalculationResult(
        decimal BaseSalary,
        decimal OvertimeAmount,
        decimal BenefitsAmount,
        decimal DiscountAmount,
        decimal GrossAmount,
        decimal NetAmount,
        decimal FgtsEmployerAmount,
        IReadOnlyList<PayrollLineResult> Lines);

    public static decimal DefaultSalaryForRole(EmployeeRole role) => role switch
    {
        EmployeeRole.Manager => 12000m,
        EmployeeRole.Nurse => 4800m,
        EmployeeRole.Technician => 3600m,
        EmployeeRole.Administrative => 3200m,
        _ => 2800m,
    };

    public static int ExpectedShiftsForRole(EmployeeRole role) => role switch
    {
        EmployeeRole.Nurse => 20,
        EmployeeRole.Technician => 20,
        EmployeeRole.Manager => 22,
        EmployeeRole.Administrative => 22,
        _ => 20,
    };

    public static decimal InsalubridadeAmount(EmployeeRole role)
    {
        var gradePercent = role switch
        {
            EmployeeRole.Nurse => 0.20m,
            EmployeeRole.Technician => 0.20m,
            EmployeeRole.Other => 0.10m,
            _ => 0m,
        };
        return Math.Round(MinimumWage * gradePercent, 2);
    }

    public static PayrollCalculationResult Calculate(PayrollCalculationInput input)
    {
        var baseSalary = input.BaseSalary > 0 ? input.BaseSalary : DefaultSalaryForRole(input.Role);
        var hourlyRate = baseSalary / MonthlyWorkHours;
        var lines = new List<PayrollLineResult>();

        lines.Add(new(PayrollLineType.Earning, "SAL", "Salário base", baseSalary));

        if (input.AbsenceDays > 0)
        {
            var absenceDiscount = Math.Round(baseSalary / 30m * input.AbsenceDays, 2);
            if (absenceDiscount > 0)
            {
                lines.Add(new(PayrollLineType.Discount, "FAL", $"Faltas ({input.AbsenceDays} dia(s))", absenceDiscount));
            }
        }

        if (input.BonusAmount > 0)
        {
            lines.Add(new(PayrollLineType.Earning, "BON", "Bônus", Math.Round(input.BonusAmount, 2)));
        }

        var extraShifts = Math.Max(0, input.TotalShiftCount - ExpectedShiftsForRole(input.Role));
        var overtimeAmount = extraShifts > 0
            ? Math.Round(extraShifts * HoursPerShift * hourlyRate * 0.50m, 2)
            : 0m;
        if (overtimeAmount > 0)
        {
            lines.Add(new(PayrollLineType.Earning, "HE", $"Horas extras ({extraShifts} plantão(ões))", overtimeAmount));
        }

        var nightPremium = input.NightShiftCount > 0
            ? Math.Round(input.NightShiftCount * HoursPerShift * hourlyRate * 0.20m, 2)
            : 0m;
        if (nightPremium > 0)
        {
            lines.Add(new(PayrollLineType.Earning, "AN", $"Adicional noturno ({input.NightShiftCount} turno(s))", nightPremium));
        }

        var insalubridade = InsalubridadeAmount(input.Role);
        if (insalubridade > 0)
        {
            lines.Add(new(PayrollLineType.Earning, "INS", "Adicional de insalubridade", insalubridade));
        }

        var benefits = Math.Round(input.ValeRefeicao, 2);
        if (benefits > 0)
        {
            lines.Add(new(PayrollLineType.Earning, "VR", "Vale-refeição", benefits));
        }

        var grossEarnings = lines
            .Where(l => l.LineType == PayrollLineType.Earning)
            .Sum(l => l.Amount);
        var absenceTotal = lines
            .Where(l => l.LineType == PayrollLineType.Discount && l.Code == "FAL")
            .Sum(l => l.Amount);
        var taxableGross = grossEarnings - absenceTotal;
        var fgtsEmployer = Math.Round(taxableGross * 0.08m, 2);

        var inss = CalculateInss(taxableGross);
        lines.Add(new(PayrollLineType.Discount, "INSS", "INSS", inss));

        var irrfBase = taxableGross - inss - (input.DependentCount * IrrfDependentDeduction);
        var irrf = CalculateIrrf(Math.Max(0, irrfBase));
        if (irrf > 0)
        {
            lines.Add(new(PayrollLineType.Discount, "IRRF", "IRRF", irrf));
        }

        if (input.ValeTransportePercent > 0)
        {
            var vt = Math.Round(taxableGross * input.ValeTransportePercent / 100m, 2);
            if (vt > 0)
            {
                lines.Add(new(PayrollLineType.Discount, "VT", $"Vale-transporte ({input.ValeTransportePercent:0.#}%)", vt));
            }
        }

        if (input.HealthPlanDiscount > 0)
        {
            lines.Add(new(PayrollLineType.Discount, "PS", "Plano de saúde", Math.Round(input.HealthPlanDiscount, 2)));
        }

        var totalDiscounts = lines.Where(l => l.LineType == PayrollLineType.Discount).Sum(l => l.Amount);
        var net = grossEarnings - totalDiscounts;

        return new PayrollCalculationResult(
            baseSalary,
            overtimeAmount,
            benefits,
            totalDiscounts,
            grossEarnings,
            net,
            fgtsEmployer,
            lines);
    }

    public static PayrollItem BuildPayrollItem(Guid employeeId, PayrollCalculationResult result)
    {
        var item = new PayrollItem
        {
            EmployeeId = employeeId,
            BaseSalary = result.BaseSalary,
            OvertimeAmount = result.OvertimeAmount,
            BenefitsAmount = result.BenefitsAmount,
            DiscountAmount = result.DiscountAmount,
            GrossAmount = result.GrossAmount,
            NetAmount = result.NetAmount,
            FgtsEmployerAmount = result.FgtsEmployerAmount,
        };

        foreach (var line in result.Lines)
        {
            item.Lines.Add(new PayrollItemLine
            {
                LineType = line.LineType,
                Code = line.Code,
                Description = line.Description,
                Amount = line.Amount,
            });
        }

        return item;
    }

    public static void RecalculateItemTotalsFromLines(PayrollItem item)
    {
        var activeLines = item.Lines.Where(l => l.IsActive).ToList();
        var earnings = activeLines.Where(l => l.LineType == PayrollLineType.Earning).Sum(l => l.Amount);
        var discounts = activeLines.Where(l => l.LineType == PayrollLineType.Discount).Sum(l => l.Amount);
        var absence = activeLines.Where(l => l.LineType == PayrollLineType.Discount && l.Code == "FAL").Sum(l => l.Amount);
        var taxableGross = earnings - absence;

        item.GrossAmount = earnings;
        item.DiscountAmount = discounts;
        item.NetAmount = earnings - discounts;
        item.OvertimeAmount = activeLines.Where(l => l.LineType == PayrollLineType.Earning && l.Code == "HE").Sum(l => l.Amount);
        item.BenefitsAmount = activeLines
            .Where(l => l.LineType == PayrollLineType.Earning && (l.Code == "VR" || l.Code == "AN" || l.Code == "INS"))
            .Sum(l => l.Amount);
        item.FgtsEmployerAmount = Math.Round(Math.Max(0, taxableGross) * 0.08m, 2);
    }

    public static decimal CalculateInss(decimal gross)
    {
        if (gross <= 0) return 0m;

        var taxable = Math.Min(gross, InssCeiling);
        decimal inss = 0m;
        decimal previous = 0m;

        inss += Bracket(taxable, previous, 1412m, 0.075m);
        previous = 1412m;
        inss += Bracket(taxable, previous, 2666.68m, 0.09m);
        previous = 2666.68m;
        inss += Bracket(taxable, previous, 4000.03m, 0.12m);
        previous = 4000.03m;
        inss += Bracket(taxable, previous, InssCeiling, 0.14m);

        return Math.Round(inss, 2);
    }

    public static decimal CalculateIrrf(decimal baseAfterDeductions)
    {
        if (baseAfterDeductions <= 2112m) return 0m;
        if (baseAfterDeductions <= 2826.65m) return Math.Round(baseAfterDeductions * 0.075m - 158.40m, 2);
        if (baseAfterDeductions <= 3751.05m) return Math.Round(baseAfterDeductions * 0.15m - 370.40m, 2);
        if (baseAfterDeductions <= 4664.68m) return Math.Round(baseAfterDeductions * 0.225m - 651.73m, 2);
        return Math.Round(baseAfterDeductions * 0.275m - 884.96m, 2);
    }

    private static decimal Bracket(decimal gross, decimal from, decimal to, decimal rate)
    {
        if (gross <= from) return 0m;
        var amount = Math.Min(gross, to) - from;
        return amount > 0 ? amount * rate : 0m;
    }
}
