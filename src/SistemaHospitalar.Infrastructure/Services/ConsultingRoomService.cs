using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.ConsultingRooms;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class ConsultingRoomService(AppDbContext dbContext) : IConsultingRoomService
{
    public async Task<IReadOnlyList<ConsultingRoomDto>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ConsultingRooms
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new ConsultingRoomDto(
                r.Id, r.Name, r.Floor, r.Building, r.Status,
                r.Specialty != null ? r.Specialty.Name : null))
            .ToListAsync(cancellationToken);
    }

    public async Task<ConsultingRoomDto> CreateRoomAsync(
        CreateConsultingRoomRequest request, CancellationToken cancellationToken = default)
    {
        var room = new ConsultingRoom
        {
            Name = request.Name.Trim(),
            Floor = request.Floor?.Trim(),
            Building = request.Building?.Trim(),
            SpecialtyId = request.SpecialtyId
        };

        dbContext.ConsultingRooms.Add(room);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetRoomsAsync(cancellationToken)).First(r => r.Id == room.Id);
    }

    public async Task<IReadOnlyList<ConsultingRoomScheduleDto>> GetSchedulesAsync(
        Guid? roomId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ConsultingRoomSchedules.AsNoTracking().Where(s => s.IsActive);

        if (roomId.HasValue)
        {
            query = query.Where(s => s.ConsultingRoomId == roomId.Value);
        }

        return await query
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(s => new ConsultingRoomScheduleDto(
                s.Id, s.ConsultingRoomId, s.ConsultingRoom.Name,
                s.ProfessionalId, s.Professional.FullName, s.Professional.Specialty.Name,
                s.DayOfWeek, s.StartTime.ToString("HH:mm"), s.EndTime.ToString("HH:mm")))
            .ToListAsync(cancellationToken);
    }

    public async Task<ConsultingRoomScheduleDto> CreateScheduleAsync(
        CreateRoomScheduleRequest request, CancellationToken cancellationToken = default)
    {
        if (!TimeOnly.TryParse(request.StartTime, out var start) || !TimeOnly.TryParse(request.EndTime, out var end))
        {
            throw new InvalidOperationException("Horário inválido.");
        }

        if (end <= start)
        {
            throw new InvalidOperationException("Horário final deve ser após o inicial.");
        }

        var schedule = new ConsultingRoomSchedule
        {
            ConsultingRoomId = request.ConsultingRoomId,
            ProfessionalId = request.ProfessionalId,
            DayOfWeek = request.DayOfWeek,
            StartTime = start,
            EndTime = end
        };

        dbContext.ConsultingRoomSchedules.Add(schedule);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetSchedulesAsync(request.ConsultingRoomId, cancellationToken))
            .First(s => s.Id == schedule.Id);
    }
}
