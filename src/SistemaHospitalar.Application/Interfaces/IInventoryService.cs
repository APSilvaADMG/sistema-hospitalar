using SistemaHospitalar.Application.DTOs.Inventory;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IInventoryService
{
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        string? search,
        bool? lowStockOnly,
        ProductType? type = null,
        CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<StockMovementDto> RegisterInboundAsync(StockInboundRequest request, CancellationToken cancellationToken = default);
    Task<StockMovementDto> RegisterOutboundAsync(StockOutboundRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(
        Guid? productId,
        string? search = null,
        StockMovementType? type = null,
        DateOnly? from = null,
        DateOnly? to = null,
        int limit = 300,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductBillingRuleDto>> GetBillingRulesAsync(
        Guid productId,
        string? priceTable,
        bool? isActive,
        CancellationToken cancellationToken = default);
    Task<ProductBillingRuleDto> CreateBillingRuleAsync(
        Guid productId,
        CreateProductBillingRuleRequest request,
        CancellationToken cancellationToken = default);
    Task<ProductBillingRuleDto> UpdateBillingRuleAsync(
        Guid ruleId,
        UpdateProductBillingRuleRequest request,
        CancellationToken cancellationToken = default);
    Task DeleteBillingRuleAsync(Guid ruleId, CancellationToken cancellationToken = default);
}

public interface IPharmacyService
{
    Task<PharmacyDispensingDto> DispenseAsync(DispenseMedicationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PharmacyDispensingDto>> GetDispensingsAsync(Guid? patientId, CancellationToken cancellationToken = default);
    Task<PharmacyDispensingReversalDto> ReverseDispensingAsync(
        Guid dispensingId,
        ReversePharmacyDispensingRequest request,
        Guid? reversedByUserId,
        CancellationToken cancellationToken = default);
}
