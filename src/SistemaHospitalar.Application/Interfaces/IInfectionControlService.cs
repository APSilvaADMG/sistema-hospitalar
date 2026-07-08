using SistemaHospitalar.Application.DTOs.InfectionControl;

namespace SistemaHospitalar.Application.Interfaces;

public interface IInfectionControlService
{
    Task<InfectionControlDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InfectionSurveillanceDto>> GetSurveillanceCasesAsync(CancellationToken cancellationToken = default);
    Task<InfectionSurveillanceDto> CreateSurveillanceCaseAsync(CreateInfectionSurveillanceRequest request, CancellationToken cancellationToken = default);
    Task<InfectionSurveillanceDto?> ResolveSurveillanceCaseAsync(Guid id, ResolveInfectionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IsolationPrecautionDto>> GetIsolationPrecautionsAsync(bool? activeOnly, CancellationToken cancellationToken = default);
    Task<IsolationPrecautionDto> CreateIsolationPrecautionAsync(CreateIsolationPrecautionRequest request, CancellationToken cancellationToken = default);
    Task<IsolationPrecautionDto?> LiftIsolationPrecautionAsync(Guid id, CancellationToken cancellationToken = default);
}
