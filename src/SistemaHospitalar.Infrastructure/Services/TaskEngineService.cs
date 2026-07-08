using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Tasks;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class TaskEngineService(AppDbContext dbContext) : ITaskEngineService
{
    public async Task<UserMissionsDto> GenerateTasksForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        var missions = new List<UserMissionDto>();

        missions.AddRange(await dbContext.PendingItems
            .AsNoTracking()
            .Where(p => p.UsuarioResponsavelId == userId && p.IsActive)
            .Where(p => p.Status == PendingItemStatus.Aberta || p.Status == PendingItemStatus.EmAndamento)
            .OrderByDescending(p => p.Prioridade)
            .ThenBy(p => p.DataLimite)
            .Take(20)
            .Select(p => new UserMissionDto(
                p.Id,
                p.Titulo,
                p.Descricao,
                p.Tipo,
                p.Prioridade,
                p.LinkDestino,
                p.DataAbertura,
                p.DataLimite,
                p.Setor,
                true))
            .ToListAsync(cancellationToken));

        var roleMissions = user.Role switch
        {
            UserRole.Reception => await BuildReceptionMissionsAsync(cancellationToken),
            UserRole.Nurse or UserRole.NursingTechnician => await BuildNurseMissionsAsync(cancellationToken),
            UserRole.Warehouse => await BuildWarehouseMissionsAsync(cancellationToken),
            UserRole.Hospitality or UserRole.Porter => await BuildHotelariaMissionsAsync(cancellationToken),
            _ => [],
        };

        foreach (var mission in roleMissions)
        {
            if (!missions.Any(m => m.Id == mission.Id))
            {
                missions.Add(mission);
            }
        }

        var ordered = missions
            .OrderByDescending(m => m.Priority)
            .ThenBy(m => m.DataLimite ?? DateTime.MaxValue)
            .Take(30)
            .ToList();

        var high = ordered.Count(m =>
            m.Priority is PendingItemPriority.Alta or PendingItemPriority.Critica);

        return new UserMissionsDto(ordered.Count, high, ordered);
    }

    public async Task<bool> CompleteTaskAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var pending = await dbContext.PendingItems
            .FirstOrDefaultAsync(
                p => p.Id == id && p.UsuarioResponsavelId == userId && p.IsActive,
                cancellationToken);

        if (pending is null)
        {
            return false;
        }

        pending.Status = PendingItemStatus.Concluida;
        pending.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<IReadOnlyList<UserMissionDto>> BuildReceptionMissionsAsync(
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);

        var pendingCheckIns = await dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.IsActive
                && a.ScheduledAt >= start
                && a.ScheduledAt < end
                && (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed))
            .OrderBy(a => a.ScheduledAt)
            .Take(8)
            .Select(a => new UserMissionDto(
                a.Id,
                $"Check-in pendente — {a.Patient.FullName}",
                $"Consulta às {a.ScheduledAt:HH:mm} com {a.Professional.FullName}",
                PendingItemType.CheckInPending,
                PendingItemPriority.Normal,
                "/recepcao/check-in",
                a.ScheduledAt,
                a.ScheduledAt,
                "Recepção",
                false))
            .ToListAsync(cancellationToken);

        return pendingCheckIns;
    }

    private async Task<IReadOnlyList<UserMissionDto>> BuildNurseMissionsAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.MedicalRecordEntries
            .AsNoTracking()
            .Where(e => e.IsActive
                && e.EntryType == MedicalRecordEntryType.Prescription
                && !e.IsSigned)
            .OrderByDescending(e => e.CreatedAt)
            .Take(10)
            .Select(e => new UserMissionDto(
                e.Id,
                "Prescrição aguardando assinatura",
                $"Paciente {e.MedicalRecord.Patient.FullName} — revisar e assinar prescrição",
                PendingItemType.UnsignedPrescription,
                PendingItemPriority.Alta,
                "/enfermagem",
                e.CreatedAt,
                e.CreatedAt.AddHours(4),
                "Enfermagem",
                false))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<UserMissionDto>> BuildWarehouseMissionsAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.MinimumStock > 0 && p.QuantityOnHand <= p.MinimumStock)
            .OrderBy(p => p.QuantityOnHand)
            .Take(10)
            .Select(p => new UserMissionDto(
                p.Id,
                $"Reposição: {p.Name}",
                $"Saldo {p.QuantityOnHand} (mín. {p.MinimumStock})",
                PendingItemType.LowStock,
                p.QuantityOnHand <= 0 ? PendingItemPriority.Critica : PendingItemPriority.Alta,
                "/estoque/dashboard",
                DateTime.UtcNow,
                null,
                "Almoxarifado",
                false))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<UserMissionDto>> BuildHotelariaMissionsAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.CleaningRequests
            .AsNoTracking()
            .Where(c => c.IsActive
                && c.Status != CleaningRequestStatus.Completed
                && c.Status != CleaningRequestStatus.Cancelled)
            .OrderByDescending(c => c.RequestedAt)
            .Take(10)
            .Select(c => new UserMissionDto(
                c.Id,
                $"Higienização — Leito {c.Bed.BedNumber}",
                $"{c.Bed.Ward.Name} · {c.CleaningType}",
                PendingItemType.BedCleaning,
                c.CleaningType == CleaningType.Terminal
                    ? PendingItemPriority.Alta
                    : PendingItemPriority.Normal,
                "/hotelaria",
                c.RequestedAt,
                c.RequestedAt.AddHours(2),
                "Hotelaria",
                false))
            .ToListAsync(cancellationToken);
    }
}
