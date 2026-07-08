using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public ProductType Type { get; set; }
    public string Unit { get; set; } = "UN";
    public decimal QuantityOnHand { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public string? Description { get; set; }
    public string? Presentation { get; set; }
    public decimal? ContentQuantity { get; set; }
    public string? Barcode { get; set; }
    public string? Category { get; set; }
    public string? Manufacturer { get; set; }
    public string? DefaultLocation { get; set; }
    public string? TussCode { get; set; }
    public int ExpiryWarningDays { get; set; }
    public decimal AveragePurchasePrice { get; set; }
    public decimal AverageSalePrice { get; set; }
    public bool AllowOutboundFromRegister { get; set; } = true;
    public string? EntryLocations { get; set; }
    public string? PhotoData { get; set; }

    public ICollection<StockMovement> Movements { get; set; } = [];
    public ICollection<ProductBillingRule> BillingRules { get; set; } = [];
    public ICollection<PharmacyDispensing> Dispensings { get; set; } = [];
    public ICollection<ProductLot> Lots { get; set; } = [];
}
