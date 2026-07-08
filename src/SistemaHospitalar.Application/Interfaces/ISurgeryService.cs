using SistemaHospitalar.Application.DTOs.Surgery;

namespace SistemaHospitalar.Application.Interfaces;

public interface ISurgeryService
{
    Task<IReadOnlyList<OperatingRoomDto>> GetOperatingRoomsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SurgeryDto>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<SurgeryDto> CreateAsync(CreateSurgeryRequest request, CancellationToken cancellationToken = default);
    Task<SurgeryDto?> UpdateStatusAsync(Guid id, UpdateSurgeryStatusRequest request, CancellationToken cancellationToken = default);
    Task<SurgeryDto?> UpdateSafetyChecklistAsync(
        Guid id,
        UpdateSurgerySafetyChecklistRequest request,
        CancellationToken cancellationToken = default);
}
