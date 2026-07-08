using SistemaHospitalar.Domain.Common;

namespace SistemaHospitalar.Domain.Entities;

public class ProductBillingRule : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string PriceTable { get; set; } = string.Empty;
    public string? ReferenceTable { get; set; }
    public string? Code { get; set; }
    public decimal PricePfb { get; set; }
    public decimal Pmc { get; set; }
    public string? Edition { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}
