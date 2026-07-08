using SistemaHospitalar.Application.DTOs.Waste;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IWasteService
{
    Task<WasteDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WasteCollectionDto>> ListAsync(
        WasteType? wasteType,
        WasteCollectionStatus? status,
        string? sector,
        CancellationToken cancellationToken = default);

    Task<WasteCollectionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WasteCollectionDto> CreateAsync(
        CreateWasteCollectionRequest request,
        CancellationToken cancellationToken = default);

    Task<WasteCollectionDto?> UpdateAsync(
        Guid id,
        UpdateWasteCollectionRequest request,
        CancellationToken cancellationToken = default);
}
