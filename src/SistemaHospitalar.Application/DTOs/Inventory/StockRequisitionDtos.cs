using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Inventory;

public record StockRequisitionItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    string ProductUnit,
    decimal QuantityOnHand,
    decimal Quantity,
    decimal FulfilledQuantity,
    StockRequisitionStatus ItemStatus,
    decimal UnitPrice,
    string? Notes);

public record StockRequisitionDto(
    Guid Id,
    int SequenceNumber,
    string RequestNumber,
    PurchaseSector RequestingSector,
    string? OriginLocation,
    string? DestinationLocation,
    string RequestedBy,
    string? RecipientName,
    StockRequisitionPriority Priority,
    DateOnly? DueDate,
    StockRequisitionStatus Status,
    DateTime RequestedAt,
    int ItemCount,
    decimal TotalQuantity);

public record StockRequisitionDetailDto(
    Guid Id,
    int SequenceNumber,
    string RequestNumber,
    PurchaseSector RequestingSector,
    string? OriginLocation,
    string? DestinationLocation,
    string RequestedBy,
    string? RecipientName,
    StockRequisitionPriority Priority,
    DateOnly? DueDate,
    string? Notes,
    StockRequisitionStatus Status,
    DateTime RequestedAt,
    IReadOnlyList<StockRequisitionItemDto> Items);

public record StockRequisitionItemRequest(
    Guid ProductId,
    decimal Quantity,
    StockRequisitionStatus ItemStatus,
    decimal UnitPrice,
    string? Notes);

public record CreateStockRequisitionRequest(
    string RequestedBy,
    string? RecipientName,
    StockRequisitionPriority Priority,
    DateOnly? DueDate,
    string? DestinationLocation,
    string? Notes,
    IReadOnlyList<StockRequisitionItemRequest> Items);

public record UpdateStockRequisitionRequest(
    string RequestedBy,
    string? RecipientName,
    StockRequisitionPriority Priority,
    DateOnly? DueDate,
    string? DestinationLocation,
    string? Notes,
    IReadOnlyList<StockRequisitionItemRequest> Items);
