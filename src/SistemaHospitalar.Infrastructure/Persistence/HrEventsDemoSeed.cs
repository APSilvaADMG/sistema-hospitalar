using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Time;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Garante eventos de RH (férias, treinamentos, avaliações) quando o seed principal não os criou.
/// </summary>
public static class HrEventsDemoSeed
{
    public const string EventMarker = "gth-hr-events-v1";

    private const int MinPerType = 8;

    public static async Task EnsureAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var employees = await db.Employees
            .Where(e => e.IsActive)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (employees.Count == 0)
        {
            return;
        }

        var today = HospitalTime.TodayInBrazil;
        var created = 0;

        created += await EnsureTypeAsync(db, employees, HrEventType.Vacation, today, cancellationToken);
        created += await EnsureTypeAsync(db, employees, HrEventType.Training, today, cancellationToken);
        created += await EnsureTypeAsync(db, employees, HrEventType.PerformanceReview, today, cancellationToken);

        if (created > 0)
        {
            logger.LogInformation("RH eventos demo: +{Count} registros.", created);
        }
    }

    private static async Task<int> EnsureTypeAsync(
        AppDbContext db,
        IReadOnlyList<Guid> employees,
        HrEventType eventType,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var existing = await db.EmployeeHrEvents.CountAsync(
            e => e.IsActive && e.EventType == eventType,
            cancellationToken);

        if (existing >= MinPerType)
        {
            return 0;
        }

        var needed = MinPerType - existing;
        var rnd = new Random((int)eventType * 1009 + today.DayNumber);
        var events = new List<EmployeeHrEvent>();

        for (var i = 0; i < needed; i++)
        {
            var employeeId = employees[rnd.Next(employees.Count)];
            var start = eventType switch
            {
                HrEventType.Vacation => today.AddDays(rnd.Next(-30, 60)),
                HrEventType.Training => today.AddDays(-rnd.Next(1, 90)),
                _ => today.AddDays(-rnd.Next(10, 120)),
            };

            events.Add(new EmployeeHrEvent
            {
                EmployeeId = employeeId,
                EventType = eventType,
                Title = TitleFor(eventType, rnd),
                Detail = DetailFor(eventType, rnd),
                StartDate = start,
                EndDate = eventType == HrEventType.Vacation
                    ? start.AddDays(rnd.Next(10, 21))
                    : eventType == HrEventType.Training ? start : null,
                Notes = EventMarker,
            });
        }

        db.EmployeeHrEvents.AddRange(events);
        await db.SaveChangesAsync(cancellationToken);
        return events.Count;
    }

    private static string TitleFor(HrEventType type, Random rnd) => type switch
    {
        HrEventType.Vacation => new[] { "Férias — julho", "Férias coletivas", "Recesso" }[rnd.Next(3)],
        HrEventType.Training => new[] { "ACLS", "NR-32", "Biossegurança", "CCIH — higienização" }[rnd.Next(4)],
        _ => new[] { "Avaliação semestral", "Feedback 360°", "Avaliação de desempenho" }[rnd.Next(3)],
    };

    private static string DetailFor(HrEventType type, Random rnd) => type switch
    {
        HrEventType.Vacation => $"{rnd.Next(10, 21)} dias — período aquisitivo",
        HrEventType.Training => $"Carga horária: {rnd.Next(4, 12)}h",
        _ => $"Nota geral: {rnd.Next(70, 96)}/100",
    };
}
