using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class ProductLot : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string BatchNumber { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public string? Manufacturer { get; set; }
    public decimal QuantityOnHand { get; set; }
    public string? LocationName { get; set; }
    public decimal? UnitCost { get; set; }
}

public class StockReceipt : BaseEntity
{
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierCnpj { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? InvoiceSeries { get; set; }
    public DateOnly? InvoiceIssueDate { get; set; }
    public string? NfeAccessKey { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public decimal FreightAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? PaymentCondition { get; set; }
    public string? Notes { get; set; }
    public string? ReceivedByUserName { get; set; }
    public ICollection<StockReceiptItem> Items { get; set; } = [];
}

public class StockReceiptItem : BaseEntity
{
    public Guid StockReceiptId { get; set; }
    public StockReceipt StockReceipt { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? ProductLotId { get; set; }
    public ProductLot? ProductLot { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Ncm { get; set; }
    public string? Cfop { get; set; }
}

public class StockIssue : BaseEntity
{
    public string SectorName { get; set; } = string.Empty;
    public string ResponsibleName { get; set; } = string.Empty;
    public StockIssueType IssueType { get; set; }
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }
    public string? Notes { get; set; }
    public ICollection<StockIssueItem> Items { get; set; } = [];
}

public class StockIssueItem : BaseEntity
{
    public Guid StockIssueId { get; set; }
    public StockIssue StockIssue { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? ProductLotId { get; set; }
    public ProductLot? ProductLot { get; set; }
    public decimal Quantity { get; set; }
}
