using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class StockRequisition : BaseEntity
{
    public int SequenceNumber { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public PurchaseSector RequestingSector { get; set; } = PurchaseSector.Administration;
    public string? OriginLocation { get; set; }
    public string? DestinationLocation { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public StockRequisitionPriority Priority { get; set; } = StockRequisitionPriority.VeryLow;
    public DateOnly? DueDate { get; set; }
    public string? Notes { get; set; }
    public StockRequisitionStatus Status { get; set; } = StockRequisitionStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StockRequisitionItem> Items { get; set; } = [];
}

public class StockRequisitionItem : BaseEntity
{
    public Guid StockRequisitionId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal FulfilledQuantity { get; set; }
    public StockRequisitionStatus ItemStatus { get; set; } = StockRequisitionStatus.Pending;
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }

    public StockRequisition StockRequisition { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
