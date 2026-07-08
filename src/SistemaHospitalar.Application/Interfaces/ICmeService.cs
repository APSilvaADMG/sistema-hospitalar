using SistemaHospitalar.Application.DTOs.Cme;

namespace SistemaHospitalar.Application.Interfaces;

public interface ICmeService
{
    Task<IReadOnlyList<InstrumentKitDto>> GetKitsAsync(CancellationToken cancellationToken = default);
    Task<InstrumentKitDto> CreateKitAsync(CreateInstrumentKitRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SterilizationCycleDto>> GetCyclesAsync(CancellationToken cancellationToken = default);
    Task<SterilizationCycleDto> CreateCycleAsync(CreateSterilizationCycleRequest request, CancellationToken cancellationToken = default);
    Task<SterilizationCycleDto?> StartCycleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SterilizationCycleDto?> CompleteCycleAsync(Guid id, CompleteSterilizationCycleRequest request, CancellationToken cancellationToken = default);
    Task<SterilizationCycleDto?> RejectSterilizationCycleAsync(Guid id, string? reason, CancellationToken cancellationToken = default);
}
