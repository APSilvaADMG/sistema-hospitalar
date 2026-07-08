using SistemaHospitalar.Application.DTOs.ConsultingRooms;

namespace SistemaHospitalar.Application.Interfaces;

public interface IConsultingRoomService
{
    Task<IReadOnlyList<ConsultingRoomDto>> GetRoomsAsync(CancellationToken cancellationToken = default);
    Task<ConsultingRoomDto> CreateRoomAsync(CreateConsultingRoomRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsultingRoomScheduleDto>> GetSchedulesAsync(Guid? roomId, CancellationToken cancellationToken = default);
    Task<ConsultingRoomScheduleDto> CreateScheduleAsync(CreateRoomScheduleRequest request, CancellationToken cancellationToken = default);
}
