using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services.Payroll;

public static class PayrollSeedHelper
{
    public static async Task<List<Employee>> GetActiveEmployeesAsync(AppDbContext db, CancellationToken cancellationToken)
        => await db.Employees.AsNoTracking().Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync(cancellationToken);

    public static async Task<Dictionary<Guid, (int TotalShifts, int NightShifts)>> GetShiftStatsAsync(
        AppDbContext db,
        IReadOnlyList<Guid> employeeIds,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
        {
            return [];
        }

        return await db.EmployeeShifts
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
            .ToDictionaryAsync(x => x.EmployeeId, x => (x.TotalShifts, x.NightShifts), cancellationToken);
    }

    public static PayrollItem BuildItemForEmployee(
        Employee employee,
        int year,
        int month,
        (int TotalShifts, int NightShifts)? shifts,
        Random? rnd = null)
    {
        rnd ??= Random.Shared;
        var baseSalary = employee.BaseSalary > 0
            ? employee.BaseSalary
            : PayrollCalculationService.DefaultSalaryForRole(employee.Role);

        var totalShifts = shifts?.TotalShifts ?? PayrollCalculationService.ExpectedShiftsForRole(employee.Role);
        var nightShifts = shifts?.NightShifts ?? rnd.Next(2, 7);

        if (shifts is null && employee.Role is EmployeeRole.Nurse or EmployeeRole.Technician)
        {
            totalShifts += rnd.Next(0, 4);
        }

        var expectedShifts = PayrollCalculationService.ExpectedShiftsForRole(employee.Role);
        var absenceDays = Math.Max(0, expectedShifts - totalShifts);

        var calc = PayrollCalculationService.Calculate(new PayrollCalculationService.PayrollCalculationInput(
            employee.Role,
            baseSalary,
            year,
            month,
            nightShifts,
            totalShifts,
            absenceDays,
            rnd.Next(350, 551),
            6m,
            rnd.Next(0, 3) == 0 ? rnd.Next(80, 201) : 0m,
            0));

        return PayrollCalculationService.BuildPayrollItem(employee.Id, calc);
    }

    public static PayrollRun BuildRun(
        int year,
        int month,
        IReadOnlyList<PayrollItem> items,
        PayrollRunStatus status,
        string? notes,
        DateTime? generatedAt = null,
        DateTime? approvedAt = null,
        DateTime? paidAt = null)
    {
        return new PayrollRun
        {
            Year = year,
            Month = month,
            ReferenceDate = new DateOnly(year, month, 1),
            Status = status,
            TotalGross = items.Sum(i => i.GrossAmount),
            TotalDiscounts = items.Sum(i => i.DiscountAmount),
            TotalNet = items.Sum(i => i.NetAmount),
            TotalFgtsEmployer = items.Sum(i => i.FgtsEmployerAmount),
            GeneratedAt = generatedAt,
            ApprovedAt = approvedAt,
            PaidAt = paidAt,
            Notes = notes,
            Items = items.ToList(),
        };
    }
}
