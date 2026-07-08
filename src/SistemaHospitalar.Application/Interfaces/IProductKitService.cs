using SistemaHospitalar.Application.DTOs.Inventory;

namespace SistemaHospitalar.Application.Interfaces;

public interface IProductKitService
{
    Task<IReadOnlyList<ProductKitDto>> GetKitsAsync(string? search, CancellationToken cancellationToken = default);
    Task<ProductKitDetailDto?> GetKitByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductKitDetailDto> CreateKitAsync(CreateProductKitRequest request, CancellationToken cancellationToken = default);
    Task<ProductKitDetailDto> UpdateKitAsync(Guid id, UpdateProductKitRequest request, CancellationToken cancellationToken = default);
    Task DeleteKitAsync(Guid id, CancellationToken cancellationToken = default);
}
