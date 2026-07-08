namespace SistemaHospitalar.Application.DTOs.Inventory;

public record ProductKitItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductSku,
    decimal Quantity,
    string? InsuranceCode,
    decimal UnitPrice,
    bool VariablePrice);

public record ProductKitDto(
    Guid Id,
    string Name,
    string? PriceTable,
    int ItemCount,
    decimal TotalUnitPrice);

public record ProductKitDetailDto(
    Guid Id,
    string Name,
    string? PriceTable,
    IReadOnlyList<ProductKitItemDto> Items);

public record ProductKitItemRequest(
    Guid ProductId,
    decimal Quantity,
    string? InsuranceCode,
    decimal UnitPrice,
    bool VariablePrice);

public record CreateProductKitRequest(
    string Name,
    string? PriceTable,
    IReadOnlyList<ProductKitItemRequest> Items);

public record UpdateProductKitRequest(
    string Name,
    string? PriceTable,
    IReadOnlyList<ProductKitItemRequest> Items);
