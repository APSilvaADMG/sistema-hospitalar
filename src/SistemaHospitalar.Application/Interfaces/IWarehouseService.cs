using SistemaHospitalar.Application.DTOs.Inventory;

namespace SistemaHospitalar.Application.Interfaces;

public interface IWarehouseService
{
    Task<WarehouseDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductLotDto>> GetLotsAsync(
        Guid? productId,
        int? expiringWithinDays,
        CancellationToken cancellationToken = default);

    Task<StockReceiptDto> CreateReceiptAsync(
        CreateStockReceiptRequest request,
        CancellationToken cancellationToken = default);

    Task<StockIssueDto> CreateIssueAsync(
        CreateStockIssueRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductLotDto>> GetExpiringLotsAsync(
        int days,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SectorConsumptionDto>> GetConsumptionBySectorAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}
