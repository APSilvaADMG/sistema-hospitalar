using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Surgery;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class SurgeryService(AppDbContext dbContext) : ISurgeryService
{
    public async Task<IReadOnlyList<OperatingRoomDto>> GetOperatingRoomsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.OperatingRooms
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new OperatingRoomDto(r.Id, r.Name, r.Status, r.Location))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SurgeryDto>> GetByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);

        return await dbContext.Surgeries
            .AsNoTracking()
            .Where(s => s.ScheduledAt >= start && s.ScheduledAt < end && s.IsActive)
            .OrderBy(s => s.ScheduledAt)
            .Select(MapToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<SurgeryDto> CreateAsync(
        CreateSurgeryRequest request,
        CancellationToken cancellationToken = default)
    {
        await PatientCareValidation.RequireEligibleForCareAsync(
            dbContext, request.PatientId, encryption: null, validateSusCns: true, cancellationToken);

        var room = await dbContext.OperatingRooms.FirstOrDefaultAsync(
            r => r.Id == request.OperatingRoomId && r.IsActive, cancellationToken);

        if (room is null)
        {
            throw new InvalidOperationException("Sala cirúrgica não encontrada.");
        }

        if (room.Status == OperatingRoomStatus.Maintenance)
        {
            throw new InvalidOperationException("Sala cirúrgica em manutenção.");
        }

        var scheduledAt = request.ScheduledAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Utc)
            : request.ScheduledAt.ToUniversalTime();

        var endTime = scheduledAt.AddMinutes(request.EstimatedDurationMinutes);
        var hasConflict = await dbContext.Surgeries.AnyAsync(s =>
            s.OperatingRoomId == request.OperatingRoomId &&
            s.IsActive &&
            s.Status != SurgeryStatus.Cancelled &&
            s.Status != SurgeryStatus.Completed &&
            s.ScheduledAt < endTime &&
            s.ScheduledAt.AddMinutes(s.EstimatedDurationMinutes) > scheduledAt,
            cancellationToken);

        if (hasConflict)
        {
            throw new InvalidOperationException("Conflito de horário na sala cirúrgica.");
        }

        var surgery = new Surgery
        {
            PatientId = request.PatientId,
            OperatingRoomId = request.OperatingRoomId,
            SurgeonId = request.SurgeonId,
            ProcedureName = request.ProcedureName.Trim(),
            ScheduledAt = scheduledAt,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            Notes = request.Notes?.Trim()
        };

        dbContext.Surgeries.Add(surgery);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(surgery.Id, cancellationToken))!;
    }

    public async Task<SurgeryDto?> UpdateStatusAsync(
        Guid id,
        UpdateSurgeryStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var surgery = await dbContext.Surgeries
            .Include(s => s.OperatingRoom)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (surgery is null)
        {
            return null;
        }

        var previousStatus = surgery.Status;
        surgery.Status = request.Status;
        surgery.UpdatedAt = DateTime.UtcNow;

        if (request.Status == SurgeryStatus.InProgress && previousStatus != SurgeryStatus.InProgress)
        {
            HospitalBusinessRules.ValidateOmsBeforeSurgeryStart(
                surgery.ConsentConfirmed,
                surgery.OmsSignInCompleted,
                surgery.OmsTimeOutCompleted);

            surgery.OperatingRoom.Status = OperatingRoomStatus.InUse;
            surgery.OperatingRoom.UpdatedAt = DateTime.UtcNow;
        }
        else if (request.Status == SurgeryStatus.Completed && previousStatus == SurgeryStatus.InProgress)
        {
            if (!surgery.OmsSignOutCompleted)
            {
                throw new InvalidOperationException(
                    $"[{BusinessRuleCodes.OmsChecklist}] Sign Out obrigatório antes de concluir a cirurgia (RN-019).");
            }
        }
        else if ((request.Status == SurgeryStatus.Completed || request.Status == SurgeryStatus.Cancelled) &&
                 previousStatus == SurgeryStatus.InProgress)
        {
            surgery.OperatingRoom.Status = OperatingRoomStatus.Available;
            surgery.OperatingRoom.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<SurgeryDto?> UpdateSafetyChecklistAsync(
        Guid id,
        UpdateSurgerySafetyChecklistRequest request,
        CancellationToken cancellationToken = default)
    {
        var surgery = await dbContext.Surgeries.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (surgery is null) return null;

        if (request.ConsentConfirmed.HasValue) surgery.ConsentConfirmed = request.ConsentConfirmed.Value;
        if (request.OmsSignInCompleted.HasValue) surgery.OmsSignInCompleted = request.OmsSignInCompleted.Value;
        if (request.OmsTimeOutCompleted.HasValue) surgery.OmsTimeOutCompleted = request.OmsTimeOutCompleted.Value;
        if (request.OmsSignOutCompleted.HasValue) surgery.OmsSignOutCompleted = request.OmsSignOutCompleted.Value;
        surgery.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task<SurgeryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Surgeries
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(MapToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<Surgery, SurgeryDto>> MapToDto() =>
        s => new SurgeryDto(
            s.Id,
            s.PatientId,
            s.Patient.FullName,
            s.OperatingRoomId,
            s.OperatingRoom.Name,
            s.SurgeonId,
            s.Surgeon.FullName,
            s.ProcedureName,
            s.ScheduledAt,
            s.EstimatedDurationMinutes,
            s.Status,
            s.Notes,
            s.ConsentConfirmed,
            s.OmsSignInCompleted,
            s.OmsTimeOutCompleted,
            s.OmsSignOutCompleted);
}
