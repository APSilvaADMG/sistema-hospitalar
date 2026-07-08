using SistemaHospitalar.Application.DTOs.Emergency;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IEmergencyService
{
    Task<IReadOnlyList<EmergencyVisitDto>> GetVisitsAsync(EmergencyVisitStatus? status, CancellationToken cancellationToken = default);
    Task<EmergencyVisitDto> CreateVisitAsync(CreateEmergencyVisitRequest request, CancellationToken cancellationToken = default);
    Task<EmergencyVisitDto?> UpdateStatusAsync(Guid id, UpdateEmergencyVisitStatusRequest request, CancellationToken cancellationToken = default);
}
