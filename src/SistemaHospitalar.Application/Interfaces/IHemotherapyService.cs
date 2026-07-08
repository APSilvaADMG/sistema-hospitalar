using SistemaHospitalar.Application.DTOs.Hemotherapy;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IHemotherapyService
{
    Task<IReadOnlyList<BloodUnitDto>> GetBloodUnitsAsync(BloodUnitStatus? status, CancellationToken cancellationToken = default);
    Task<BloodUnitDto> CreateBloodUnitAsync(CreateBloodUnitRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransfusionRequestDto>> GetTransfusionRequestsAsync(CancellationToken cancellationToken = default);
    Task<TransfusionRequestDto> CreateTransfusionRequestAsync(CreateTransfusionRequestRequest request, CancellationToken cancellationToken = default);
    Task<TransfusionRequestDto?> MatchTransfusionAsync(Guid requestId, MatchTransfusionRequest request, CancellationToken cancellationToken = default);
    Task<TransfusionRequestDto?> CompleteTransfusionAsync(Guid requestId, CancellationToken cancellationToken = default);
}
