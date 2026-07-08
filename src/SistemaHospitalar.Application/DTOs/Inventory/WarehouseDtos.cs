using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Inventory;

public record WarehouseDashboardDto(
    int TotalProducts,
    int LowStockCount,
    int ExpiringLotsCount,
    decimal TodayInboundQuantity,
    decimal TodayOutboundQuantity,
    int PendingRequisitions);

public record ProductLotDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    string BatchNumber,
    DateOnly? ExpiryDate,
    string? Manufacturer,
    decimal QuantityOnHand,
    string? LocationName,
    decimal? UnitCost,
    bool IsExpiringSoon,
    DateTime CreatedAt);

public record StockReceiptItemRequest(
    Guid ProductId,
    string BatchNumber,
    DateOnly? ExpiryDate,
    decimal Quantity,
    decimal UnitPrice,
    string? Manufacturer,
    string? LocationName,
    string? Ncm,
    string? Cfop);

public record CreateStockReceiptRequest(
    string SupplierName,
    string? SupplierCnpj,
    string? InvoiceNumber,
    string? InvoiceSeries,
    DateOnly? InvoiceIssueDate,
    string? NfeAccessKey,
    DateTime? ReceivedAt,
    decimal FreightAmount,
    decimal DiscountAmount,
    string? PaymentCondition,
    string? Notes,
    string? ReceivedByUserName,
    IReadOnlyList<StockReceiptItemRequest> Items);

public record StockReceiptDto(
    Guid Id,
    string SupplierName,
    string? SupplierCnpj,
    string? InvoiceNumber,
    string? InvoiceSeries,
    DateOnly? InvoiceIssueDate,
    string? NfeAccessKey,
    DateTime ReceivedAt,
    decimal TotalAmount,
    decimal FreightAmount,
    decimal DiscountAmount,
    string? PaymentCondition,
    string? Notes,
    string? ReceivedByUserName,
    IReadOnlyList<StockReceiptItemDto> Items);

public record StockReceiptItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid? ProductLotId,
    string BatchNumber,
    DateOnly? ExpiryDate,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? Ncm,
    string? Cfop);

public record StockIssueItemRequest(
    Guid ProductId,
    decimal Quantity,
    Guid? ProductLotId);

public record CreateStockIssueRequest(
    string SectorName,
    string ResponsibleName,
    StockIssueType IssueType,
    Guid? PatientId,
    Guid? HospitalizationId,
    string? Notes,
    string? UserName,
    IReadOnlyList<StockIssueItemRequest> Items);

public record StockIssueDto(
    Guid Id,
    string SectorName,
    string ResponsibleName,
    StockIssueType IssueType,
    Guid? PatientId,
    Guid? HospitalizationId,
    string? Notes,
    DateTime CreatedAt,
    IReadOnlyList<StockIssueItemDto> Items);

public record StockIssueItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid? ProductLotId,
    string? BatchNumber,
    decimal Quantity);

public record SectorConsumptionDto(
    string SectorName,
    decimal TotalQuantity,
    int MovementCount);

public record DenyStockRequisitionRequest(string Reason);
