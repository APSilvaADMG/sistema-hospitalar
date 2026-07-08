using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaHospitalar.Application.Common;
using SistemaHospitalar.Application.DTOs.Appointments;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Time;

namespace SistemaHospitalar.Infrastructure.Services;

public class AppointmentService(
    AppDbContext dbContext,
    IFinancialAccountService financialAccountService,
    IConnectAppointmentIntegration connectIntegration,
    ClinicalStatusAuditLogger clinicalStatusAuditLogger) : IAppointmentService
{
    public async Task<IReadOnlyList<AppointmentDto>> GetByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = HospitalTime.BrazilDayRangeUtc(date);

        return await dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.ScheduledAt >= start && a.ScheduledAt < end && a.IsActive)
            .OrderBy(a => a.ScheduledAt)
            .Select(MapToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<AppointmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(MapToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CreateAppointmentResultDto> CreateAsync(
        CreateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        await PatientCareValidation.RequireEligibleForCareAsync(
            dbContext, request.PatientId, encryption: null, validateSusCns: true, cancellationToken);

        var warnings = new List<string>();
        await ValidateEligibilityAsync(request, warnings, cancellationToken);

        if (warnings.Count > 0 && !request.IgnoreEligibilityWarning)
        {
            var blocking = warnings.Any(w => w.StartsWith("[ELIGIBILITY_BLOCK]", StringComparison.Ordinal));
            if (blocking)
            {
                throw new InvalidOperationException(warnings[0].Replace("[ELIGIBILITY_BLOCK] ", string.Empty));
            }
        }

        var professionalExists = await dbContext.Professionals.AnyAsync(p => p.Id == request.ProfessionalId && p.IsActive, cancellationToken);

        if (!professionalExists)
        {
            throw new InvalidOperationException("Profissional não encontrado.");
        }

        var scheduledAt = NormalizeScheduledAt(request.ScheduledAt);
        var durationMinutes = NormalizeDurationMinutes(request.DurationMinutes);
        var room = request.Room?.Trim();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await AppointmentScheduleGuard.EnsureNoConflictAsync(
                dbContext,
                request.ProfessionalId,
                scheduledAt,
                durationMinutes,
                excludeAppointmentId: null,
                room,
                cancellationToken);

            var appointment = new Appointment
            {
                PatientId = request.PatientId,
                ProfessionalId = request.ProfessionalId,
                ScheduledAt = scheduledAt,
                DurationMinutes = durationMinutes,
                Reason = request.Reason?.Trim(),
                Notes = request.Notes?.Trim(),
                Room = room
            };

            dbContext.Appointments.Add(appointment);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await connectIntegration.OnAppointmentCreatedAsync(appointment.Id, cancellationToken);

            var dto = (await GetByIdAsync(appointment.Id, cancellationToken))!;
            return new CreateAppointmentResultDto(dto, warnings);
        }
        catch (DbUpdateException ex) when (IsScheduleOverlapViolation(ex))
        {
            throw new ScheduleConflictException();
        }

    }

    private async Task ValidateEligibilityAsync(
        CreateAppointmentRequest request,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var patientInsurance = await dbContext.PatientInsurances
            .AsNoTracking()
            .Include(pi => pi.HealthInsurance)
            .Where(pi => pi.PatientId == request.PatientId && pi.IsActive)
            .OrderByDescending(pi => pi.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken);

        if (patientInsurance?.HealthInsurance is null)
        {
            return;
        }

        var insurer = patientInsurance.HealthInsurance;
        var name = insurer.Name.ToUpperInvariant();
        if (name.Contains("SUS") || name.Contains("PARTICULAR") || name.Contains("AVULSO"))
        {
            return;
        }

        if (!insurer.RequiresEligibilityCheck)
        {
            return;
        }

        var lastCheck = await dbContext.InsuranceEligibilityChecks
            .AsNoTracking()
            .Where(e => e.PatientId == request.PatientId
                && e.HealthInsuranceId == insurer.Id
                && e.IsActive)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var stale = lastCheck is null
            || DateTime.UtcNow - lastCheck.CreatedAt > TimeSpan.FromHours(24);

        if (lastCheck?.Status == EligibilityStatus.Ineligible)
        {
            warnings.Add("[ELIGIBILITY_BLOCK] Paciente com elegibilidade negada no convênio. Verifique a carteirinha antes de agendar.");
            return;
        }

        if (stale)
        {
            warnings.Add($"[ELIGIBILITY_WARN] Elegibilidade do convênio {insurer.Name} não verificada nas últimas 24h. Recomenda-se consultar o operador antes do atendimento.");
        }
    }

    public async Task<AppointmentDto?> UpdateAsync(
        Guid id,
        UpdateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var appointment = await dbContext.Appointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (appointment is null)
        {
            return null;
        }

        var professionalId = request.ProfessionalId ?? appointment.ProfessionalId;
        var scheduledAt = request.ScheduledAt.HasValue
            ? NormalizeScheduledAt(request.ScheduledAt.Value)
            : appointment.ScheduledAt;
        var durationMinutes = request.DurationMinutes.HasValue
            ? NormalizeDurationMinutes(request.DurationMinutes.Value)
            : appointment.DurationMinutes;

        var scheduleChanged = professionalId != appointment.ProfessionalId
            || scheduledAt != appointment.ScheduledAt
            || durationMinutes != appointment.DurationMinutes;

        var room = request.Room?.Trim() ?? appointment.Room;
        var reasonChanged = request.Reason is not null;
        var roomChanged = request.Room is not null;
        var needsConflictCheck = scheduleChanged || roomChanged;

        if (!needsConflictCheck && !reasonChanged)
        {
            return await GetByIdAsync(id, cancellationToken);
        }

        if (scheduleChanged)
        {
            var professionalExists = await dbContext.Professionals.AnyAsync(
                p => p.Id == professionalId && p.IsActive,
                cancellationToken);

            if (!professionalExists)
            {
                throw new InvalidOperationException("Profissional não encontrado.");
            }
        }

        await using var transaction = needsConflictCheck
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            if (needsConflictCheck)
            {
                await AppointmentScheduleGuard.EnsureNoConflictAsync(
                    dbContext,
                    professionalId,
                    scheduledAt,
                    durationMinutes,
                    excludeAppointmentId: id,
                    room,
                    cancellationToken);
            }

            if (scheduleChanged)
            {
                appointment.ProfessionalId = professionalId;
                appointment.ScheduledAt = scheduledAt;
                appointment.DurationMinutes = durationMinutes;
            }

            if (reasonChanged)
            {
                appointment.Reason = request.Reason!.Trim();
            }

            if (roomChanged)
            {
                appointment.Room = request.Room!.Trim();
            }

            appointment.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateException ex) when (IsScheduleOverlapViolation(ex))
        {
            throw new ScheduleConflictException();
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<AppointmentDto?> UpdateStatusAsync(
        Guid id,
        UpdateAppointmentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var appointment = await dbContext.Appointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (appointment is null)
        {
            return null;
        }

        AttendanceRules.ValidateCancellationJustification(request.Status, request.CancellationReason);

        var previousStatus = appointment.Status;
        appointment.Status = request.Status;
        appointment.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.Status != previousStatus)
        {
            await clinicalStatusAuditLogger.LogStatusChangeAsync(
                "Appointment",
                appointment.Id,
                "AlterarStatusAgendamento",
                previousStatus.ToString(),
                request.Status.ToString(),
                cancellationToken: cancellationToken);
        }

        if (request.Status == Domain.Enums.AppointmentStatus.Completed &&
            previousStatus != Domain.Enums.AppointmentStatus.Completed)
        {
            await financialAccountService.CreateFromAppointmentAsync(id, 250m, cancellationToken);
        }

        if (request.Status == Domain.Enums.AppointmentStatus.Cancelled &&
            previousStatus != Domain.Enums.AppointmentStatus.Cancelled)
        {
            await connectIntegration.OnAppointmentCancelledAsync(id, cancellationToken);
        }
        else if (request.Status != previousStatus)
        {
            await connectIntegration.OnAppointmentStatusChangedAsync(id, cancellationToken);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    private static DateTime NormalizeScheduledAt(DateTime scheduledAt) =>
        scheduledAt.Kind switch
        {
            DateTimeKind.Utc => scheduledAt,
            DateTimeKind.Local => scheduledAt.ToUniversalTime(),
            _ => HospitalTime.BrazilLocalToUtc(scheduledAt)
        };

    private static int NormalizeDurationMinutes(int durationMinutes) =>
        durationMinutes > 0 ? durationMinutes : AppointmentDurationRules.ConsultaMinutes;

    private static bool IsScheduleOverlapViolation(DbUpdateException ex)
    {
        if (ex.InnerException is not PostgresException pg)
        {
            return false;
        }

        if (pg.SqlState == PostgresErrorCodes.ExclusionViolation)
        {
            return true;
        }

        return pg.SqlState == PostgresErrorCodes.UniqueViolation
            && pg.ConstraintName?.Contains("appointments_professional", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static System.Linq.Expressions.Expression<Func<Appointment, AppointmentDto>> MapToDto() =>
        a => new AppointmentDto(
            a.Id,
            a.PatientId,
            a.Patient.FullName,
            a.ProfessionalId,
            a.Professional.FullName,
            a.Professional.Specialty.Name,
            a.ScheduledAt,
            a.DurationMinutes,
            a.Status,
            a.Reason,
            a.Room);
}
