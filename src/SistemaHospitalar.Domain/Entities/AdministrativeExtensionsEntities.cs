using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class TpaAdministrator : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal DiscountPercent { get; set; }

    public ICollection<TpaClaim> Claims { get; set; } = [];
}

public class TpaClaim : BaseEntity
{
    public Guid TpaAdministratorId { get; set; }
    public TpaAdministrator TpaAdministrator { get; set; } = null!;

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid? HealthInsuranceId { get; set; }
    public HealthInsurance? HealthInsurance { get; set; }

    public DateOnly ServiceDate { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public TpaClaimStatus Status { get; set; } = TpaClaimStatus.Draft;
    public string? Notes { get; set; }
    public Guid? FinancialAccountId { get; set; }
    public FinancialAccount? FinancialAccount { get; set; }
}

public class PayrollRun : BaseEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public DateOnly ReferenceDate { get; set; }
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;
    public decimal TotalGross { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalNet { get; set; }
    public decimal TotalFgtsEmployer { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public Guid? ConsolidatedFinancialAccountId { get; set; }
    public FinancialAccount? ConsolidatedFinancialAccount { get; set; }

    public ICollection<PayrollItem> Items { get; set; } = [];
}

public class PayrollItem : BaseEntity
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun PayrollRun { get; set; } = null!;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public decimal BaseSalary { get; set; }
    public decimal OvertimeAmount { get; set; }
    public decimal BenefitsAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal FgtsEmployerAmount { get; set; }
    public Guid? FinancialAccountId { get; set; }
    public FinancialAccount? FinancialAccount { get; set; }

    public ICollection<PayrollItemLine> Lines { get; set; } = [];
}

public class PayrollItemLine : BaseEntity
{
    public Guid PayrollItemId { get; set; }
    public PayrollItem PayrollItem { get; set; } = null!;

    public PayrollLineType LineType { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class PharmacyBillingEntry : BaseEntity
{
    public Guid DispensingId { get; set; }
    public PharmacyDispensing Dispensing { get; set; } = null!;

    public PharmacyBillingPayerType PayerType { get; set; }
    public Guid? HealthInsuranceId { get; set; }
    public HealthInsurance? HealthInsurance { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public bool Paid { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? FinancialAccountId { get; set; }
    public FinancialAccount? FinancialAccount { get; set; }
    public string? Notes { get; set; }
}

public class BirthRegistration : BaseEntity
{
    public Guid MotherPatientId { get; set; }
    public Patient MotherPatient { get; set; } = null!;

    public string BabyName { get; set; } = string.Empty;
    public DateTime BirthAt { get; set; }
    public decimal WeightKg { get; set; }
    public decimal HeightCm { get; set; }
    public string? Notes { get; set; }
}

