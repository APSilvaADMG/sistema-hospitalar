using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class StockMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public StockMovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? PatientOrSupplier { get; set; }
    public string? ResponsibleName { get; set; }
    public string? UserName { get; set; }
    public string? BatchNumber { get; set; }
    public string? IndividualCode { get; set; }
    public string? Location { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Account { get; set; }
}
