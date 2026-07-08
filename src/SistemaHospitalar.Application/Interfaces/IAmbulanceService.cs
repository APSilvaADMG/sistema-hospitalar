using SistemaHospitalar.Application.DTOs.Ambulance;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IAmbulanceService
{
    Task<IReadOnlyList<AmbulanceDto>> GetAmbulancesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AmbulanceDispatchDto>> GetDispatchesAsync(AmbulanceDispatchStatus? status, CancellationToken cancellationToken = default);
    Task<AmbulanceDispatchDto> CreateDispatchAsync(CreateAmbulanceDispatchRequest request, CancellationToken cancellationToken = default);
    Task<AmbulanceDispatchDto?> AssignAmbulanceAsync(Guid dispatchId, AssignAmbulanceRequest request, CancellationToken cancellationToken = default);
    Task<AmbulanceDispatchDto?> UpdateDispatchStatusAsync(Guid dispatchId, UpdateDispatchStatusRequest request, CancellationToken cancellationToken = default);
}
