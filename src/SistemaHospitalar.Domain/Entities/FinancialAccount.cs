using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class FinancialAccount : BaseEntity
{
    public FinancialAccountDirection Direction { get; set; } = FinancialAccountDirection.Receivable;

    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public string? CounterpartyName { get; set; }

    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public FinancialAccountCategory Category { get; set; } = FinancialAccountCategory.Other;
    public string? Notes { get; set; }
    public string? InvoiceNumber { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public FinancialAccountStatus Status { get; set; } = FinancialAccountStatus.Open;
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public PaymentMethod? ExpectedPaymentMethod { get; set; }

    public Guid? HealthInsuranceId { get; set; }
    public HealthInsurance? HealthInsurance { get; set; }

    public Guid? TissGuideId { get; set; }
    public TissGuide? TissGuide { get; set; }

    public Guid? ParentFinancialAccountId { get; set; }
    public FinancialAccount? ParentFinancialAccount { get; set; }
    public int? InstallmentNumber { get; set; }
    public int? InstallmentCount { get; set; }

    public ICollection<FinancialPayment> Payments { get; set; } = [];
    public ICollection<FinancialAccount> InstallmentAccounts { get; set; } = [];
    public ICollection<FinancialAccountLineItem> LineItems { get; set; } = [];
}
