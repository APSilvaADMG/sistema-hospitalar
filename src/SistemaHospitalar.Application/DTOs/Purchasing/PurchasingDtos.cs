using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Purchasing;

public record SupplierDto(
    Guid Id,
    string Name,
    string? Cnpj,
    string? Email,
    string? Phone,
    string? ContactName);

public record CreateSupplierRequest(
    string Name,
    string? Cnpj,
    string? Email,
    string? Phone,
    string? ContactName);

public record PurchaseOrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    int ReceivedQuantity,
    decimal UnitPrice,
    decimal Total);

public record PurchaseOrderDto(
    Guid Id,
    string OrderNumber,
    Guid SupplierId,
    string SupplierName,
    PurchaseSector Sector,
    PurchasePriority Priority,
    string RequestedBy,
    string? Justification,
    PurchaseOrderStatus Status,
    DateTime OrderedAt,
    DateTime? ExpectedAt,
    decimal TotalAmount,
    string? Notes,
    IReadOnlyList<PurchaseOrderItemDto> Items);

public record PurchaseOrderItemRequest(Guid ProductId, int Quantity, decimal UnitPrice);

public record CreatePurchaseOrderRequest(
    Guid SupplierId,
    PurchaseSector Sector,
    PurchasePriority Priority,
    string RequestedBy,
    string? Justification,
    DateTime? ExpectedAt,
    string? Notes,
    IReadOnlyList<PurchaseOrderItemRequest> Items);

public record ReceivePurchaseOrderRequest(
    IReadOnlyList<ReceivePurchaseOrderItemRequest> Items);

public record ReceivePurchaseOrderItemRequest(Guid ItemId, int Quantity);

public record PurchaseSectorPresetDto(
    PurchaseSector Sector,
    string Label,
    string Description,
    int SuggestedDeliveryDays);

public record PurchaseSuggestedItemDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    string Unit,
    decimal QuantityOnHand,
    decimal MinimumStock,
    bool IsLowStock,
    int SuggestedQuantity,
    decimal SuggestedUnitPrice,
    string Reason);

public record PurchaseCreateSuggestionsDto(
    IReadOnlyList<PurchaseSectorPresetDto> Sectors,
    PurchaseSector SelectedSector,
    Guid? SuggestedSupplierId,
    int SuggestedDeliveryDays,
    IReadOnlyList<PurchaseSuggestedItemDto> LowStockItems,
    IReadOnlyList<PurchaseSuggestedItemDto> KitItems,
    IReadOnlyList<SupplierDto> Suppliers);
