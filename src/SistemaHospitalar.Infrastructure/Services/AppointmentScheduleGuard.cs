using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.Common;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

/// <summary>
/// Validação centralizada de conflitos de agenda (profissional e sala) com lock transacional.
/// </summary>
public static class AppointmentScheduleGuard
{
    public static async Task EnsureNoConflictAsync(
        AppDbContext db,
        Guid professionalId,
        DateTime scheduledAt,
        int durationMinutes,
        Guid? excludeAppointmentId,
        string? room,
        CancellationToken cancellationToken = default)
    {
        await AcquireProfessionalLockAsync(db, professionalId, cancellationToken);

        var hasProfessionalConflict = await HasProfessionalConflictAsync(
            db,
            professionalId,
            scheduledAt,
            durationMinutes,
            excludeAppointmentId,
            cancellationToken);

        if (hasProfessionalConflict)
        {
            throw new ScheduleConflictException();
        }

        var normalizedRoom = room?.Trim();
        if (string.IsNullOrEmpty(normalizedRoom))
        {
            return;
        }

        var hasRoomConflict = await HasRoomConflictAsync(
            db,
            normalizedRoom,
            scheduledAt,
            durationMinutes,
            excludeAppointmentId,
            cancellationToken);

        if (hasRoomConflict)
        {
            throw new ScheduleConflictException(
                "Este consultório/sala já possui agendamento neste horário.");
        }
    }

    public static async Task<bool> HasProfessionalConflictAsync(
        AppDbContext db,
        Guid professionalId,
        DateTime scheduledAt,
        int durationMinutes,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken)
    {
        return await db.Appointments.AnyAsync(a =>
            a.Id != excludeAppointmentId &&
            a.ProfessionalId == professionalId &&
            AppointmentSchedulingEngine.IsBlocking(a.Status, a.IsActive) &&
            AppointmentSchedulingEngine.IntervalsOverlap(
                a.ScheduledAt,
                NormalizeDurationMinutes(a.DurationMinutes),
                scheduledAt,
                durationMinutes),
            cancellationToken);
    }

    public static async Task<bool> HasRoomConflictAsync(
        AppDbContext db,
        string room,
        DateTime scheduledAt,
        int durationMinutes,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken)
    {
        return await db.Appointments.AnyAsync(a =>
            a.Id != excludeAppointmentId &&
            a.Room != null &&
            a.Room.Trim() == room &&
            AppointmentSchedulingEngine.IsBlocking(a.Status, a.IsActive) &&
            AppointmentSchedulingEngine.IntervalsOverlap(
                a.ScheduledAt,
                NormalizeDurationMinutes(a.DurationMinutes),
                scheduledAt,
                durationMinutes),
            cancellationToken);
    }

    public static async Task AcquireProfessionalLockAsync(
        AppDbContext db,
        Guid professionalId,
        CancellationToken cancellationToken)
    {
        var lockKey = AdvisoryLockKey(professionalId);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({lockKey})",
            cancellationToken);
    }

    internal static long AdvisoryLockKey(Guid id)
    {
        var bytes = id.ToByteArray();
        return BitConverter.ToInt64(bytes, 0);
    }

    private static int NormalizeDurationMinutes(int durationMinutes) =>
        durationMinutes > 0 ? durationMinutes : AppointmentDurationRules.ConsultaMinutes;
}
