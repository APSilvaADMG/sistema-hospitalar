using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Services;

public enum AppointmentKind
{
    Consulta = 1,
    Retorno = 2,
    Exame = 3
}

public static class AppointmentDurationRules
{
    public const int ConsultaMinutes = 30;
    public const int RetornoMinutes = 15;
    public const int ExameMinutes = 45;

    public static int GetMinutes(AppointmentKind kind) => kind switch
    {
        AppointmentKind.Retorno => RetornoMinutes,
        AppointmentKind.Exame => ExameMinutes,
        _ => ConsultaMinutes
    };
}

public sealed class SchedulingBusinessHours
{
    public TimeOnly DayStart { get; init; } = new(8, 0);
    public TimeOnly DayEnd { get; init; } = new(18, 0);
    public int SlotStepMinutes { get; init; } = 15;

    public bool IsWorkingDay(DateOnly date)
        => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
}

public readonly record struct AppointmentConflictCheck(
    DateTime ScheduledAt,
    int DurationMinutes,
    AppointmentStatus Status,
    bool IsActive = true);

public static class AppointmentSchedulingEngine
{
    public static bool HasConflict(
        IEnumerable<AppointmentConflictCheck> existingAppointments,
        Guid professionalId,
        DateTime start,
        int durationMinutes)
    {
        var endTime = start.AddMinutes(durationMinutes);

        foreach (var appointment in existingAppointments)
        {
            if (!IsBlocking(appointment))
            {
                continue;
            }

            if (appointment.ScheduledAt < endTime
                && appointment.ScheduledAt.AddMinutes(appointment.DurationMinutes) > start)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IntervalsOverlap(
        DateTime existingStart,
        int existingDurationMinutes,
        DateTime candidateStart,
        int candidateDurationMinutes)
    {
        var candidateEnd = candidateStart.AddMinutes(candidateDurationMinutes);
        return existingStart < candidateEnd
            && existingStart.AddMinutes(existingDurationMinutes) > candidateStart;
    }

    public static bool IsBlocking(AppointmentConflictCheck appointment)
        => appointment.IsActive
            && appointment.Status is not AppointmentStatus.Cancelled
                and not AppointmentStatus.NoShow;

    public static bool IsBlocking(AppointmentStatus status, bool isActive = true)
        => isActive
            && status is not AppointmentStatus.Cancelled
                and not AppointmentStatus.NoShow;
}

public sealed class ProfessionalSlotAllocator
{
    private readonly Dictionary<Guid, List<AppointmentConflictCheck>> _occupiedByProfessional = new();
    private readonly SchedulingBusinessHours _businessHours;

    public ProfessionalSlotAllocator(SchedulingBusinessHours? businessHours = null)
    {
        _businessHours = businessHours ?? new SchedulingBusinessHours();
    }

    public IReadOnlyList<AppointmentConflictCheck> GetOccupiedSlots(Guid professionalId)
        => _occupiedByProfessional.TryGetValue(professionalId, out var slots)
            ? slots
            : [];

    public void RegisterSlot(
        Guid professionalId,
        DateTime scheduledAt,
        int durationMinutes,
        AppointmentStatus status = AppointmentStatus.Scheduled,
        bool isActive = true)
    {
        if (!AppointmentSchedulingEngine.IsBlocking(status, isActive))
        {
            return;
        }

        if (!_occupiedByProfessional.TryGetValue(professionalId, out var slots))
        {
            slots = [];
            _occupiedByProfessional[professionalId] = slots;
        }

        slots.Add(new AppointmentConflictCheck(scheduledAt, durationMinutes, status, isActive));
    }

    public void SeedFromExisting(IEnumerable<(Guid ProfessionalId, DateTime ScheduledAt, int DurationMinutes, AppointmentStatus Status, bool IsActive)> appointments)
    {
        foreach (var (professionalId, scheduledAt, durationMinutes, status, isActive) in appointments)
        {
            RegisterSlot(professionalId, scheduledAt, durationMinutes, status, isActive);
        }
    }

    public bool TryAllocateSlot(
        Guid professionalId,
        DateTime preferredStartUtc,
        AppointmentKind kind,
        out DateTime allocatedStartUtc,
        Random? random = null)
    {
        allocatedStartUtc = default;
        var durationMinutes = AppointmentDurationRules.GetMinutes(kind);
        var rng = random ?? Random.Shared;
        var cursor = AlignToBusinessHours(preferredStartUtc, rng);

        for (var attempt = 0; attempt < 10_000; attempt++)
        {
            if (!AppointmentSchedulingEngine.HasConflict(
                    GetOccupiedSlots(professionalId),
                    professionalId,
                    cursor,
                    durationMinutes))
            {
                var localEnd = DateTime.SpecifyKind(cursor, DateTimeKind.Utc).ToLocalTime()
                    .AddMinutes(durationMinutes);
                var localEndTime = TimeOnly.FromDateTime(localEnd);
                var localDate = DateOnly.FromDateTime(cursor.ToLocalTime());

                if (_businessHours.IsWorkingDay(localDate) && localEndTime <= _businessHours.DayEnd)
                {
                    allocatedStartUtc = DateTime.SpecifyKind(cursor, DateTimeKind.Utc);
                    RegisterSlot(professionalId, allocatedStartUtc, durationMinutes);
                    return true;
                }
            }

            cursor = AdvanceCursor(cursor, durationMinutes, rng);
        }

        return false;
    }

    private DateTime AlignToBusinessHours(DateTime utcStart, Random rng)
    {
        var local = utcStart.ToLocalTime();
        var date = DateOnly.FromDateTime(local);

        while (!_businessHours.IsWorkingDay(date))
        {
            date = date.AddDays(1);
            local = date.ToDateTime(_businessHours.DayStart, DateTimeKind.Local);
        }

        var time = TimeOnly.FromDateTime(local);
        if (time < _businessHours.DayStart)
        {
            local = date.ToDateTime(_businessHours.DayStart, DateTimeKind.Local);
        }
        else if (time >= _businessHours.DayEnd)
        {
            date = date.AddDays(1);
            while (!_businessHours.IsWorkingDay(date))
            {
                date = date.AddDays(1);
            }

            local = date.ToDateTime(_businessHours.DayStart, DateTimeKind.Local);
        }

        var alignedMinutes = (local.Minute / _businessHours.SlotStepMinutes) * _businessHours.SlotStepMinutes;
        local = local.Date.AddHours(local.Hour).AddMinutes(alignedMinutes);

        if (rng.NextDouble() < 0.3)
        {
            local = local.AddMinutes(_businessHours.SlotStepMinutes * rng.Next(0, 3));
        }

        return local.ToUniversalTime();
    }

    private DateTime AdvanceCursor(DateTime utcCursor, int durationMinutes, Random rng)
    {
        var step = Math.Max(_businessHours.SlotStepMinutes, durationMinutes);
        var next = utcCursor.AddMinutes(step + (rng.NextDouble() < 0.2 ? _businessHours.SlotStepMinutes : 0));
        var local = next.ToLocalTime();
        var date = DateOnly.FromDateTime(local);
        var time = TimeOnly.FromDateTime(local);

        if (!_businessHours.IsWorkingDay(date) || time >= _businessHours.DayEnd)
        {
            date = date.AddDays(1);
            while (!_businessHours.IsWorkingDay(date))
            {
                date = date.AddDays(1);
            }

            return date.ToDateTime(_businessHours.DayStart, DateTimeKind.Local).ToUniversalTime();
        }

        return next;
    }
}
