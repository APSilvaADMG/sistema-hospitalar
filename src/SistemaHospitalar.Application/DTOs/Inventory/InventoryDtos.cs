using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Inventory;

public record ProductDto(
    Guid Id,
    string Name,
    string Sku,
    ProductType Type,
    string Unit,
    decimal QuantityOnHand,
    decimal MinimumStock,
    bool IsLowStock,
    string? Presentation = null,
    string? Barcode = null,
    string? Category = null,
    decimal AverageSalePrice = 0,
    string? Manufacturer = null,
    string? DefaultLocation = null);

public record ProductDetailDto(
    Guid Id,
    string Name,
    string Sku,
    ProductType Type,
    string Unit,
    decimal QuantityOnHand,
    decimal MinimumStock,
    decimal MaximumStock,
    bool IsLowStock,
    string? Description,
    string? Presentation,
    decimal? ContentQuantity,
    string? Barcode,
    string? Category,
    string? Manufacturer,
    string? DefaultLocation,
    string? TussCode,
    int ExpiryWarningDays,
    decimal AveragePurchasePrice,
    decimal AverageSalePrice,
    bool AllowOutboundFromRegister,
    string? EntryLocations,
    string? PhotoData);

public record StockMovementDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    StockMovementType Type,
    decimal Quantity,
    string Reason,
    string? Reference,
    DateTime CreatedAt,
    string? PatientOrSupplier = null,
    string? ResponsibleName = null,
    string? UserName = null,
    string? BatchNumber = null,
    string? IndividualCode = null,
    string? Location = null,
    DateOnly? ExpiryDate = null,
    string? InvoiceNumber = null,
    decimal? UnitPrice = null,
    string? Account = null);

public record ProductBillingRuleDto(
    Guid Id,
    Guid ProductId,
    string PriceTable,
    string? ReferenceTable,
    string? Code,
    decimal PricePfb,
    decimal Pmc,
    string? Edition,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool IsActive);

public record CreateProductBillingRuleRequest(
    string PriceTable,
    string? ReferenceTable,
    string? Code,
    decimal PricePfb,
    decimal Pmc,
    string? Edition,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool IsActive = true);

public record UpdateProductBillingRuleRequest(
    string PriceTable,
    string? ReferenceTable,
    string? Code,
    decimal PricePfb,
    decimal Pmc,
    string? Edition,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool IsActive);

public record CreateProductRequest(
    string Name,
    string Sku,
    ProductType Type,
    string Unit,
    decimal MinimumStock,
    string? Description,
    string? Presentation,
    decimal? ContentQuantity,
    string? Barcode,
    string? Category,
    string? Manufacturer,
    string? DefaultLocation,
    string? TussCode,
    decimal MaximumStock,
    int ExpiryWarningDays,
    decimal AveragePurchasePrice,
    decimal AverageSalePrice,
    bool AllowOutboundFromRegister,
    string? EntryLocations,
    string? PhotoData);

public record UpdateProductRequest(
    string Name,
    ProductType Type,
    string Unit,
    decimal MinimumStock,
    string? Description,
    string? Presentation,
    decimal? ContentQuantity,
    string? Barcode,
    string? Category,
    string? Manufacturer,
    string? DefaultLocation,
    string? TussCode,
    decimal MaximumStock,
    int ExpiryWarningDays,
    decimal AveragePurchasePrice,
    decimal AverageSalePrice,
    bool AllowOutboundFromRegister,
    string? EntryLocations,
    string? PhotoData);

public record StockInboundRequest(
    Guid ProductId,
    decimal Quantity,
    string Reason,
    string? Reference,
    string? PatientOrSupplier = null,
    string? ResponsibleName = null,
    string? UserName = null,
    string? BatchNumber = null,
    string? IndividualCode = null,
    string? Location = null,
    DateOnly? ExpiryDate = null,
    string? InvoiceNumber = null,
    decimal? UnitPrice = null,
    string? Account = null);

public record StockOutboundRequest(
    Guid ProductId,
    decimal Quantity,
    string Reason,
    string? PatientOrSupplier = null,
    string? ResponsibleName = null,
    string? UserName = null,
    string? BatchNumber = null,
    string? IndividualCode = null,
    string? Location = null,
    string? InvoiceNumber = null,
    decimal? UnitPrice = null,
    string? Account = null);

public record DispenseMedicationRequest(
    Guid PatientId,
    Guid ProductId,
    decimal Quantity,
    Guid? ProfessionalId,
    Guid? HospitalizationId,
    string? Notes);

public record PharmacyDispensingDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal ReversedQuantity,
    string? ProfessionalName,
    DateTime DispensedAt,
    string? Notes);

public record ReversePharmacyDispensingRequest(
    decimal Quantity,
    string? Reason);

public record PharmacyDispensingReversalDto(
    Guid Id,
    Guid DispensingId,
    decimal Quantity,
    string? Reason,
    DateTime ReversedAt);
