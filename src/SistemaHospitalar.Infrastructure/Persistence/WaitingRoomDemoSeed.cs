using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Services;
using SistemaHospitalar.Infrastructure.Time;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Garante fila da sala de espera e PS para o dia operacional (Brasil), independente dos demais seeds.
/// </summary>
public static class WaitingRoomDemoSeed
{
    public const string AppointmentMarkerPrefix = "gth-waiting-room";
    public const string EmergencyMarkerPrefix = "gth-emergency-queue";

    private const int MinAppointmentsToday = 16;
    private const int MinCompletedToday = 8;
    private const int MinEmergencyWaitingToday = 12;

    public static async Task EnsureAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var today = HospitalTime.TodayInBrazil;
        var appointmentKey = $"{AppointmentMarkerPrefix}-{today:yyyyMMdd}";
        var emergencyKey = $"{EmergencyMarkerPrefix}-{today:yyyyMMdd}";
        var (todayStart, todayEnd) = HospitalTime.BrazilDayRangeUtc(today);

        var appointmentsCreated = await EnsureAppointmentsAsync(
            db, today, appointmentKey, todayStart, todayEnd, logger, cancellationToken);

        var completedEnsured = await EnsureCompletedTodayAsync(
            db, today, appointmentKey, todayStart, todayEnd, logger, cancellationToken);

        var emergencyCreated = await EnsureEmergencyQueueAsync(
            db, emergencyKey, todayStart, todayEnd, logger, cancellationToken);

        if (appointmentsCreated > 0 || completedEnsured > 0 || emergencyCreated > 0)
        {
            logger.LogInformation(
                "Sala de espera: +{Appointments} agendamentos, +{Completed} finalizados hoje, +{Emergency} PS aguardando ({Date:yyyy-MM-dd}).",
                appointmentsCreated,
                completedEnsured,
                emergencyCreated,
                today);
        }
    }

    private static async Task<int> EnsureAppointmentsAsync(
        AppDbContext db,
        DateOnly today,
        string todayKey,
        DateTime todayStart,
        DateTime todayEnd,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var existingToday = await db.Appointments.CountAsync(
            a => a.IsActive
                && a.Notes != null
                && a.Notes.StartsWith(AppointmentMarkerPrefix)
                && a.ScheduledAt >= todayStart
                && a.ScheduledAt < todayEnd,
            cancellationToken);

        if (existingToday >= MinAppointmentsToday)
        {
            return 0;
        }

        await db.Appointments
            .Where(a => a.Notes != null
                && a.Notes.StartsWith(AppointmentMarkerPrefix)
                && a.ScheduledAt >= todayStart
                && a.ScheduledAt < todayEnd)
            .ExecuteDeleteAsync(cancellationToken);

        var patients = await db.Patients.AsNoTracking().Where(p => p.IsActive).Take(40).Select(p => p.Id).ToListAsync(cancellationToken);
        var professionals = await db.Professionals.AsNoTracking().Take(12).Select(p => p.Id).ToListAsync(cancellationToken);

        if (patients.Count == 0 || professionals.Count == 0)
        {
            logger.LogWarning("Sala de espera: sem pacientes ou profissionais para gerar agendamentos.");
            return 0;
        }

        var existingSlots = await db.Appointments
            .AsNoTracking()
            .Where(a => a.IsActive
                && a.ScheduledAt >= todayStart
                && a.ScheduledAt < todayEnd)
            .Select(a => new
            {
                a.ProfessionalId,
                a.ScheduledAt,
                a.DurationMinutes,
                a.Status,
                a.IsActive
            })
            .ToListAsync(cancellationToken);

        var allocator = new ProfessionalSlotAllocator();
        allocator.SeedFromExisting(existingSlots.Select(a =>
            (a.ProfessionalId, a.ScheduledAt, a.DurationMinutes, a.Status, a.IsActive)));

        var rnd = new Random(today.DayOfYear + today.Year);
        var reasons = new[]
        {
            "Consulta — retorno", "Consulta — primeira vez", "Exame de sangue", "Eletrocardiograma",
            "Ultrassom abdome", "Avaliação cardiológica", "Dor abdominal", "Check-up anual",
            "Renovação de receita", "Pré-operatório", "Fisioterapia — avaliação", "Nutrição clínica",
        };

        var statuses = new[]
        {
            AppointmentStatus.Scheduled,
            AppointmentStatus.Confirmed,
            AppointmentStatus.Confirmed,
            AppointmentStatus.InProgress,
            AppointmentStatus.Completed,
            AppointmentStatus.Completed,
        };

        var appointments = new List<Appointment>();
        var nowUtc = DateTime.UtcNow;

        for (var i = 0; i < MinAppointmentsToday; i++)
        {
            var status = statuses[rnd.Next(statuses.Length)];
            var minutesAgo = status switch
            {
                AppointmentStatus.Completed => rnd.Next(75, 241),
                AppointmentStatus.InProgress => rnd.Next(10, 61),
                _ => rnd.Next(5, 46),
            };
            var preferredUtc = nowUtc.AddMinutes(-minutesAgo);
            var durationMinutes = rnd.Next(2, 4) == 0 ? 60 : AppointmentDurationRules.ConsultaMinutes;
            var kind = durationMinutes == 60 ? AppointmentKind.Exame : AppointmentKind.Consulta;

            Guid? professionalId = null;
            DateTime allocatedStart = default;

            foreach (var candidateProfessionalId in professionals.OrderBy(_ => rnd.Next()))
            {
                if (allocator.TryAllocateSlot(candidateProfessionalId, preferredUtc, kind, out allocatedStart, rnd))
                {
                    professionalId = candidateProfessionalId;
                    break;
                }
            }

            if (professionalId is null)
            {
                continue;
            }

            appointments.Add(new Appointment
            {
                PatientId = patients[rnd.Next(patients.Count)],
                ProfessionalId = professionalId.Value,
                ScheduledAt = allocatedStart,
                DurationMinutes = durationMinutes,
                Status = status,
                Reason = reasons[rnd.Next(reasons.Length)],
                Room = $"Sala {(i % 6) + 1}",
                Notes = todayKey,
            });
        }

        if (appointments.Count == 0)
        {
            logger.LogWarning("Sala de espera: não foi possível alocar horários sem conflito para os profissionais disponíveis.");
            return 0;
        }

        db.Appointments.AddRange(appointments);
        await db.SaveChangesAsync(cancellationToken);
        return appointments.Count;
    }

    /// <summary>
    /// Garante atendimentos finalizados no dia operacional (KPI "Finalizados hoje" na sala de espera).
    /// Roda mesmo quando a fila já atingiu o mínimo — bases antigas não tinham status Concluído.
    /// </summary>
    private static async Task<int> EnsureCompletedTodayAsync(
        AppDbContext db,
        DateOnly today,
        string todayKey,
        DateTime todayStart,
        DateTime todayEnd,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var completedCount = await db.Appointments.CountAsync(
            a => a.IsActive
                && a.Status == AppointmentStatus.Completed
                && a.ScheduledAt >= todayStart
                && a.ScheduledAt < todayEnd,
            cancellationToken);

        if (completedCount >= MinCompletedToday)
        {
            return 0;
        }

        var needed = MinCompletedToday - completedCount;
        var nowUtc = DateTime.UtcNow;
        var repaired = 0;

        var candidates = await db.Appointments
            .Where(a => a.IsActive
                && a.Notes != null
                && a.Notes.StartsWith(AppointmentMarkerPrefix)
                && a.ScheduledAt >= todayStart
                && a.ScheduledAt < todayEnd
                && a.Status != AppointmentStatus.Completed
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow
                && a.ScheduledAt <= nowUtc.AddMinutes(-30))
            .OrderBy(a => a.ScheduledAt)
            .Take(needed)
            .ToListAsync(cancellationToken);

        foreach (var appointment in candidates)
        {
            appointment.Status = AppointmentStatus.Completed;
            appointment.UpdatedAt = nowUtc;
            repaired++;
        }

        if (repaired >= needed)
        {
            await db.SaveChangesAsync(cancellationToken);
            return repaired;
        }

        needed -= repaired;

        var patients = await db.Patients.AsNoTracking().Where(p => p.IsActive).Take(40).Select(p => p.Id).ToListAsync(cancellationToken);
        var professionals = await db.Professionals.AsNoTracking().Take(12).Select(p => p.Id).ToListAsync(cancellationToken);

        if (patients.Count == 0 || professionals.Count == 0)
        {
            if (repaired > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
            }

            return repaired;
        }

        var existingSlots = await db.Appointments
            .AsNoTracking()
            .Where(a => a.IsActive
                && a.ScheduledAt >= todayStart
                && a.ScheduledAt < todayEnd)
            .Select(a => new { a.ProfessionalId, a.ScheduledAt, a.DurationMinutes, a.Status, a.IsActive })
            .ToListAsync(cancellationToken);

        var allocator = new ProfessionalSlotAllocator();
        allocator.SeedFromExisting(existingSlots.Select(a =>
            (a.ProfessionalId, a.ScheduledAt, a.DurationMinutes, a.Status, a.IsActive)));

        var rnd = new Random(today.DayOfYear + today.Year + 7919);
        var created = new List<Appointment>();

        for (var i = 0; i < needed; i++)
        {
            var minutesAgo = rnd.Next(90, 300);
            var preferredUtc = nowUtc.AddMinutes(-minutesAgo);

            Guid? professionalId = null;
            DateTime allocatedStart = default;

            foreach (var candidateProfessionalId in professionals.OrderBy(_ => rnd.Next()))
            {
                if (allocator.TryAllocateSlot(candidateProfessionalId, preferredUtc, AppointmentKind.Consulta, out allocatedStart, rnd))
                {
                    professionalId = candidateProfessionalId;
                    break;
                }
            }

            if (professionalId is null)
            {
                continue;
            }

            created.Add(new Appointment
            {
                PatientId = patients[rnd.Next(patients.Count)],
                ProfessionalId = professionalId.Value,
                ScheduledAt = allocatedStart,
                DurationMinutes = AppointmentDurationRules.ConsultaMinutes,
                Status = AppointmentStatus.Completed,
                Reason = "Consulta — atendimento finalizado",
                Room = $"Sala {(i % 6) + 1}",
                Notes = todayKey,
            });
        }

        if (created.Count > 0)
        {
            db.Appointments.AddRange(created);
        }

        if (repaired > 0 || created.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        var total = repaired + created.Count;
        if (total > 0)
        {
            logger.LogInformation(
                "Sala de espera: {Repaired} convertidos e {Created} criados como finalizados hoje.",
                repaired,
                created.Count);
        }

        return total;
    }

    private static async Task<int> EnsureEmergencyQueueAsync(
        AppDbContext db,
        string todayKey,
        DateTime todayStart,
        DateTime todayEnd,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var existingWaiting = await db.EmergencyVisits.CountAsync(
            v => v.IsActive
                && v.Status == EmergencyVisitStatus.Waiting
                && v.Notes != null
                && v.Notes.StartsWith(EmergencyMarkerPrefix)
                && v.ArrivedAt >= todayStart
                && v.ArrivedAt < todayEnd,
            cancellationToken);

        if (existingWaiting >= MinEmergencyWaitingToday)
        {
            return 0;
        }

        await db.EmergencyVisits
            .Where(v => v.Notes != null
                && v.Notes.StartsWith(EmergencyMarkerPrefix)
                && v.ArrivedAt >= todayStart
                && v.ArrivedAt < todayEnd)
            .ExecuteDeleteAsync(cancellationToken);

        var patients = await db.Patients.AsNoTracking().Where(p => p.IsActive).Take(30).Select(p => p.Id).ToListAsync(cancellationToken);
        if (patients.Count == 0)
        {
            logger.LogWarning("PS: sem pacientes para fila de emergência.");
            return 0;
        }

        var rnd = new Random(todayKey.GetHashCode());
        var complaints = new[]
        {
            "Dor torácica", "Febre e mal-estar", "Trauma em membro inferior", "Cefaleia intensa",
            "Dispneia", "Dor abdominal", "Hipertensão elevada", "Corte em mão",
            "Crise asmática", "Tontura", "Vômitos persistentes", "Queda com suspeita de fratura",
        };

        var urgencies = new[]
        {
            TriageUrgency.High,
            TriageUrgency.Medium,
            TriageUrgency.Medium,
            TriageUrgency.Low,
            TriageUrgency.Emergency,
        };

        var visits = new List<EmergencyVisit>();
        var now = DateTime.UtcNow;

        for (var i = 0; i < MinEmergencyWaitingToday; i++)
        {
            var arrived = now.AddMinutes(-rnd.Next(10, 180));
            visits.Add(new EmergencyVisit
            {
                PatientId = patients[rnd.Next(patients.Count)],
                ChiefComplaint = complaints[rnd.Next(complaints.Length)],
                Urgency = urgencies[rnd.Next(urgencies.Length)],
                Status = EmergencyVisitStatus.Waiting,
                ArrivedAt = arrived,
                Notes = todayKey,
            });
        }

        db.EmergencyVisits.AddRange(visits);
        await db.SaveChangesAsync(cancellationToken);
        return visits.Count;
    }
}
