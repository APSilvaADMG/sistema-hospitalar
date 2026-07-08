using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class PixCharge : BaseEntity
{
    public Guid FinancialAccountId { get; set; }
    public FinancialAccount FinancialAccount { get; set; } = null!;

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public string TxId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PixChargeStatus Status { get; set; } = PixChargeStatus.Pending;
    public string CopyPasteCode { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PayerName { get; set; }
    public string? ProviderReference { get; set; }
}
