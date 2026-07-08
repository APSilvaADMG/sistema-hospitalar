using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class FinancialPayment : BaseEntity
{
    public Guid FinancialAccountId { get; set; }
    public FinancialAccount FinancialAccount { get; set; } = null!;

    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime PaidAt { get; set; }
    public string? Notes { get; set; }

    public Guid? PixChargeId { get; set; }
    public PixCharge? PixCharge { get; set; }

    public int? InstallmentCount { get; set; }
    public ICollection<FinancialPaymentInstallment> Installments { get; set; } = [];
}
