using SistemaHospitalar.Domain.Common;

namespace SistemaHospitalar.Domain.Entities;

public class ProductKit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Tabela de preço / convênio (Feegow: Tabela).</summary>
    public string? PriceTable { get; set; }

    public ICollection<ProductKitItem> Items { get; set; } = [];
}

public class ProductKitItem : BaseEntity
{
    public Guid ProductKitId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string? InsuranceCode { get; set; }
    public decimal UnitPrice { get; set; }
    public bool VariablePrice { get; set; }

    public ProductKit ProductKit { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
