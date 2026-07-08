using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Services;

/// <summary>Métricas operacionais da sala de espera (espelha a lógica do frontend Feegow).</summary>
public static class WaitingRoomStatsCalculator
{
    private const int AvgConsultationMinutes = 12;
    private const int MaxDisplayWaitMinutes = 90;

    public record AppointmentQueueItem(
        AppointmentStatus Status,
        DateTime ScheduledAt,
        string? Room = null,
        string? PatientName = null);

    public record RoomSummary(string Room, int Waiting, int InCare, string? NextPatient);

    public record WaitingRoomStats(
        int Waiting,
        int InCare,
        int CompletedToday,
        int AvgWaitMinutes,
        IReadOnlyList<RoomSummary> ByRoom);

    public static WaitingRoomStats Compute(IReadOnlyList<AppointmentQueueItem> appointments, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var nowMs = new DateTimeOffset(now).ToUnixTimeMilliseconds();

        var waiting = appointments.Count(a => IsWaitingStatus(a.Status));
        var inCare = appointments.Count(a => a.Status == AppointmentStatus.InProgress);
        var completedToday = appointments.Count(a => a.Status == AppointmentStatus.Completed);

        var waitingAppts = appointments.Where(a => IsWaitingStatus(a.Status)).ToList();
        var sortedWaiting = waitingAppts.OrderBy(a => a.ScheduledAt).ToList();
        var avgWaitMinutes = sortedWaiting.Count == 0
            ? 0
            : (int)Math.Round(
                sortedWaiting.Select((appt, index) => EstimatePatientWaitMinutes(appt, index + 1, nowMs))
                    .Average());

        var roomMap = new Dictionary<string, (int Waiting, int InCare, AppointmentQueueItem? Next)>(StringComparer.OrdinalIgnoreCase);
        foreach (var appt in appointments)
        {
            var room = string.IsNullOrWhiteSpace(appt.Room) ? "Sem sala" : appt.Room.Trim();
            roomMap.TryGetValue(room, out var entry);
            if (IsWaitingStatus(appt.Status))
            {
                entry.Waiting += 1;
                if (entry.Next is null || appt.ScheduledAt < entry.Next.ScheduledAt)
                {
                    entry.Next = appt;
                }
            }
            else if (appt.Status == AppointmentStatus.InProgress)
            {
                entry.InCare += 1;
            }

            roomMap[room] = entry;
        }

        var byRoom = roomMap
            .Select(kvp => new RoomSummary(
                kvp.Key,
                kvp.Value.Waiting,
                kvp.Value.InCare,
                kvp.Value.Next?.PatientName))
            .OrderBy(r => r.Room, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("pt-BR"), false))
            .ToList();

        return new WaitingRoomStats(waiting, inCare, completedToday, avgWaitMinutes, byRoom);
    }

    private static bool IsWaitingStatus(AppointmentStatus status) =>
        status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed;

    private static double EstimatePatientWaitMinutes(AppointmentQueueItem appt, int queuePosition, long nowMs)
    {
        if (appt.Status == AppointmentStatus.Confirmed)
        {
            var scheduledMs = new DateTimeOffset(appt.ScheduledAt).ToUnixTimeMilliseconds();
            var elapsed = Math.Max(0, (nowMs - scheduledMs) / 60_000.0);
            return Math.Min(elapsed, MaxDisplayWaitMinutes);
        }

        return Math.Min(queuePosition * AvgConsultationMinutes, MaxDisplayWaitMinutes);
    }
}
