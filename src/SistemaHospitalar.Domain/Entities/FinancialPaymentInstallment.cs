using SistemaHospitalar.Domain.Common;

namespace SistemaHospitalar.Domain.Entities;

public class FinancialPaymentInstallment : BaseEntity
{
    public Guid FinancialPaymentId { get; set; }
    public FinancialPayment FinancialPayment { get; set; } = null!;

    public int InstallmentNumber { get; set; }
    public int InstallmentCount { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }

    public Guid? FinancialAccountId { get; set; }
    public FinancialAccount? FinancialAccount { get; set; }
}
