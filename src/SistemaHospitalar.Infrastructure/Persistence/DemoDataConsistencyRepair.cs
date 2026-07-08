using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Services;
using SistemaHospitalar.Infrastructure.Services.Payroll;
using SistemaHospitalar.Infrastructure.Time;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Corrige dados parciais deixados por seeds idempotentes que retornaram cedo demais.
/// Executado ao final da inicialização para evitar folha incompleta, estoque zerado, etc.
/// </summary>
public static class DemoDataConsistencyRepair
{
    private static readonly string[] DemoProductSkus =
    [
        "MED-DIP500", "MED-SF500", "MED-OME20", "MED-PAR750", "SUP-LUV-M", "SUP-GAZ", "MAT-SRG10",
        "MAT-AG257", "MAT-CAT18", "CCIH-DESINF", "HOTEL-LEN", "CIR-KIT01", "MAT-MASK3", "MAT-EQMAC",
        "SUP-ALC500", "PRD-MON01", "PRD-BOMBA01", "PRD-CADEIRA01", "PRD-OXI01", "PRD-TERM01",
        "GER-ESC01", "GER-UNIF01", "GER-CRACHA01", "GER-ETIQ01", "GER-PAPEL01",
        "LAB-REA01", "IMG-CON100", "LAV-ROUP", "ENG-MANUT", "NUT-DIETA",
    ];

    public static async Task EnsureAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var salariesFixed = await RepairEmployeeSalariesAsync(db, cancellationToken);
        var stockFixed = await RepairDemoProductStockAsync(db, cancellationToken);
        var metadataFixed = await WarehouseDemoSeed.RepairProductMetadataAsync(db, cancellationToken);
        var lotsFixed = await RepairOrphanProductLotsAsync(db, cancellationToken);
        var payrollFixed = await RepairIncompletePayrollRunsAsync(db, logger, cancellationToken);
        var waitingRoomFixed = await RepairWaitingRoomAppointmentsAsync(db, logger, cancellationToken);
        var duplicatesFixed = await RepairDuplicateAppointmentsAsync(db, logger, cancellationToken);

        if (salariesFixed > 0 || stockFixed > 0 || metadataFixed > 0 || lotsFixed > 0 || payrollFixed > 0 || waitingRoomFixed || duplicatesFixed > 0)
        {
            logger.LogInformation(
                "DemoDataConsistencyRepair: {Salaries} salários, {Stock} produtos, {Metadata} metadados, {Lots} lotes, {Payroll} folhas corrigidos, waiting room reparado={WaitingRoom}, conflitos de agenda={Duplicates}.",
                salariesFixed,
                stockFixed,
                metadataFixed,
                lotsFixed,
                payrollFixed,
                waitingRoomFixed,
                duplicatesFixed);
        }
    }

    private static async Task<int> RepairEmployeeSalariesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var employees = await db.Employees
            .Where(e => e.IsActive && e.BaseSalary <= 0)
            .ToListAsync(cancellationToken);

        if (employees.Count == 0)
        {
            return 0;
        }

        var rnd = new Random(20260716);
        foreach (var employee in employees)
        {
            employee.BaseSalary = PayrollCalculationService.DefaultSalaryForRole(employee.Role) + rnd.Next(-300, 501);
            employee.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return employees.Count;
    }

    private static async Task<int> RepairDemoProductStockAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var products = await db.Products
            .Where(p => p.IsActive && DemoProductSkus.Contains(p.Sku) && p.QuantityOnHand <= 0)
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            return 0;
        }

        foreach (var product in products)
        {
            product.QuantityOnHand = Math.Max(product.MinimumStock * 2, 10);
            product.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return products.Count;
    }

    private static async Task<int> RepairOrphanProductLotsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var lots = await db.ProductLots
            .Include(l => l.Product)
            .Where(l => l.IsActive && l.QuantityOnHand <= 0 && l.Product.IsActive && l.Product.QuantityOnHand > 0)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (lots.Count == 0)
        {
            return 0;
        }

        foreach (var lot in lots)
        {
            lot.QuantityOnHand = Math.Min(25, Math.Max(5, lot.Product.QuantityOnHand / 4));
            lot.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return lots.Count;
    }

    private static async Task<int> RepairIncompletePayrollRunsAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var employees = await PayrollSeedHelper.GetActiveEmployeesAsync(db, cancellationToken);
        if (employees.Count < 2)
        {
            return 0;
        }

        var expectedCount = employees.Count;

        var runs = await db.PayrollRuns
            .Include(r => r.Items)
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Month)
            .Take(12)
            .ToListAsync(cancellationToken);

        var repaired = 0;
        var runsToUpdate = new List<PayrollRun>();

        foreach (var run in runs)
        {
            var activeItems = run.Items.Where(i => i.IsActive).ToList();
            if (activeItems.Count >= expectedCount)
            {
                continue;
            }

            var presentIds = activeItems.Select(i => i.EmployeeId).ToHashSet();
            var missing = employees.Where(e => !presentIds.Contains(e.Id)).ToList();
            if (missing.Count == 0)
            {
                continue;
            }

            logger.LogWarning(
                "Folha {Month:D2}/{Year} com {Current}/{Expected} colaboradores — adicionando {Missing} ausentes.",
                run.Month,
                run.Year,
                activeItems.Count,
                expectedCount,
                missing.Count);

            var periodStart = new DateOnly(run.Year, run.Month, 1);
            var periodEnd = new DateOnly(run.Year, run.Month, DateTime.DaysInMonth(run.Year, run.Month));
            var shiftStats = await PayrollSeedHelper.GetShiftStatsAsync(
                db,
                missing.Select(e => e.Id).ToList(),
                periodStart,
                periodEnd,
                cancellationToken);

            var rnd = new Random(run.Year * 100 + run.Month);
            foreach (var employee in missing)
            {
                shiftStats.TryGetValue(employee.Id, out var shifts);
                run.Items.Add(PayrollSeedHelper.BuildItemForEmployee(employee, run.Year, run.Month, shifts, rnd));
            }

            RecalculateRunTotals(run);
            runsToUpdate.Add(run);
            repaired++;
        }

        foreach (var run in runsToUpdate)
        {
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Folha {Month:D2}/{Year} não pôde ser reparada — ignorando.", run.Month, run.Year);
                db.ChangeTracker.Clear();
            }
        }

        return repaired;
    }

    private static async Task<bool> RepairWaitingRoomAppointmentsAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var today = HospitalTime.TodayInBrazil;
        var (todayStart, todayEnd) = HospitalTime.BrazilDayRangeUtc(today);

        var count = await db.Appointments.CountAsync(
            a => a.IsActive
                && a.Notes != null
                && a.Notes.StartsWith(WaitingRoomDemoSeed.AppointmentMarkerPrefix)
                && a.ScheduledAt >= todayStart
                && a.ScheduledAt < todayEnd,
            cancellationToken);

        var completedToday = await db.Appointments.CountAsync(
            a => a.IsActive
                && a.Status == Domain.Enums.AppointmentStatus.Completed
                && a.ScheduledAt >= todayStart
                && a.ScheduledAt < todayEnd,
            cancellationToken);

        if (count >= 10 && completedToday >= 8)
        {
            return false;
        }

        logger.LogWarning(
            "Sala de espera: {Count} agendamentos e {Completed} finalizados hoje — reexecutando seed de demonstração.",
            count,
            completedToday);

        await WaitingRoomDemoSeed.EnsureAsync(db, logger, cancellationToken);
        return true;
    }

    private const string DuplicateRepairMarker = "|repaired-schedule-conflict";

    private static async Task<int> RepairDuplicateAppointmentsAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var activeAppointments = await db.Appointments
            .Where(a => a.IsActive
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow)
            .OrderBy(a => a.ProfessionalId)
            .ThenBy(a => a.ScheduledAt)
            .ThenBy(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.ProfessionalId,
                a.ScheduledAt,
                a.DurationMinutes,
                a.Notes
            })
            .ToListAsync(cancellationToken);

        if (activeAppointments.Count < 2)
        {
            return 0;
        }

        var occupiedByProfessional = new Dictionary<Guid, List<(DateTime Start, int Duration)>>();
        var reschedules = new List<(Guid Id, DateTime NewStart, string? Notes)>();

        foreach (var appointment in activeAppointments)
        {
            if (!occupiedByProfessional.TryGetValue(appointment.ProfessionalId, out var occupied))
            {
                occupied = [];
                occupiedByProfessional[appointment.ProfessionalId] = occupied;
            }

            var duration = appointment.DurationMinutes > 0
                ? appointment.DurationMinutes
                : AppointmentDurationRules.ConsultaMinutes;
            var start = appointment.ScheduledAt;
            var hasConflict = occupied.Any(slot =>
                AppointmentSchedulingEngine.IntervalsOverlap(
                    slot.Start,
                    slot.Duration,
                    start,
                    duration));

            if (hasConflict)
            {
                var shifted = start;
                var resolved = false;
                for (var attempt = 1; attempt <= 96; attempt++)
                {
                    shifted = start.AddMinutes(15 * attempt);
                    if (!occupied.Any(slot =>
                            AppointmentSchedulingEngine.IntervalsOverlap(
                                slot.Start,
                                slot.Duration,
                                shifted,
                                duration)))
                    {
                        resolved = true;
                        break;
                    }
                }

                if (!resolved)
                {
                    logger.LogWarning(
                        "Conflito de agenda não resolvido para agendamento {AppointmentId} (profissional {ProfessionalId}).",
                        appointment.Id,
                        appointment.ProfessionalId);
                    continue;
                }

                var notes = appointment.Notes;
                if (notes is null || !notes.Contains(DuplicateRepairMarker, StringComparison.Ordinal))
                {
                    notes = string.Concat(notes, DuplicateRepairMarker);
                }

                reschedules.Add((appointment.Id, shifted, notes));
                start = shifted;
            }

            occupied.Add((start, duration));
        }

        if (reschedules.Count == 0)
        {
            return 0;
        }

        foreach (var (id, newStart, notes) in reschedules)
        {
            var entity = await db.Appointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (entity is null)
            {
                continue;
            }

            entity.ScheduledAt = newStart;
            entity.Notes = notes;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogWarning(
            "Reagendados {Count} agendamento(s) com conflito de horário para o mesmo profissional.",
            reschedules.Count);

        return reschedules.Count;
    }

    private static void RecalculateRunTotals(PayrollRun run)
    {
        var items = run.Items.Where(i => i.IsActive).ToList();
        run.TotalGross = items.Sum(i => i.GrossAmount);
        run.TotalDiscounts = items.Sum(i => i.DiscountAmount);
        run.TotalNet = items.Sum(i => i.NetAmount);
        run.TotalFgtsEmployer = items.Sum(i => i.FgtsEmployerAmount);
        run.UpdatedAt = DateTime.UtcNow;
    }
}
