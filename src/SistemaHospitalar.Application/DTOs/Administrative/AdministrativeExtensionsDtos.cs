using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Administrative;

public record TpaAdministratorDto(
    Guid Id,
    string Name,
    string? Cnpj,
    string? ContactName,
    string? ContactEmail,
    decimal CommissionPercent,
    decimal DiscountPercent,
    int ClaimsCount);

public record CreateTpaAdministratorRequest(
    string Name,
    string? Cnpj,
    string? ContactName,
    string? ContactEmail,
    decimal CommissionPercent,
    decimal DiscountPercent);

public record TpaClaimDto(
    Guid Id,
    Guid TpaAdministratorId,
    string TpaAdministratorName,
    Guid PatientId,
    string PatientName,
    Guid? HealthInsuranceId,
    string? HealthInsuranceName,
    DateOnly ServiceDate,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal DiscountAmount,
    decimal NetAmount,
    TpaClaimStatus Status,
    string? Notes,
    Guid? FinancialAccountId);

public record CreateTpaClaimRequest(
    Guid TpaAdministratorId,
    Guid PatientId,
    Guid? HealthInsuranceId,
    DateOnly ServiceDate,
    decimal GrossAmount,
    decimal? CommissionPercent,
    decimal? DiscountPercent,
    string? Notes);

public record UpdateTpaClaimStatusRequest(TpaClaimStatus Status, bool CreateFinancialAccountWhenPaid = false);

public record TpaReportDto(
    int TotalClaims,
    decimal GrossTotal,
    decimal NetTotal,
    IReadOnlyList<TpaClaimDto> LatestClaims);

public record PayrollItemLineDto(
    Guid Id,
    PayrollLineType LineType,
    string Code,
    string Description,
    decimal Amount);

public record PayrollItemDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string? JobTitle,
    string DepartmentName,
    decimal BaseSalary,
    decimal OvertimeAmount,
    decimal BenefitsAmount,
    decimal DiscountAmount,
    decimal GrossAmount,
    decimal NetAmount,
    decimal FgtsEmployerAmount,
    Guid? FinancialAccountId,
    IReadOnlyList<PayrollItemLineDto> Lines);

public record PayrollRunDto(
    Guid Id,
    int Year,
    int Month,
    DateOnly ReferenceDate,
    PayrollRunStatus Status,
    decimal TotalGross,
    decimal TotalDiscounts,
    decimal TotalNet,
    decimal TotalFgtsEmployer,
    DateTime? GeneratedAt,
    DateTime? ApprovedAt,
    DateTime? PaidAt,
    string? Notes,
    Guid? ConsolidatedFinancialAccountId,
    IReadOnlyList<PayrollItemDto> Items);

public record GeneratePayrollRunRequest(
    int Year,
    int Month,
    decimal? DefaultBaseSalary,
    decimal ValeRefeicao,
    decimal ValeTransportePercent,
    decimal HealthPlanDiscount,
    int DependentCount,
    string? Notes);

public record UpdatePayrollRunStatusRequest(PayrollRunStatus Status, bool CreateFinancialAccountsWhenPaid = false);

public record PayrollItemLineInputDto(
    PayrollLineType LineType,
    string Code,
    string Description,
    decimal Amount);

public record UpdatePayrollItemLinesRequest(
    IReadOnlyList<PayrollItemLineInputDto> Lines);

public record PayrollSlipDto(
    Guid PayrollRunId,
    int Year,
    int Month,
    DateOnly ReferenceDate,
    PayrollRunStatus Status,
    PayrollItemDto Item,
    decimal TotalFgtsEmployer,
    IReadOnlyList<PayrollItemLineDto> Earnings,
    IReadOnlyList<PayrollItemLineDto> Discounts);

public record PayrollDepartmentSummaryDto(
    string DepartmentName,
    int EmployeeCount,
    decimal TotalGross,
    decimal TotalNet);

public record PayrollMonthlySummaryDto(
    int Year,
    int Month,
    PayrollRunStatus? Status,
    Guid? RunId,
    int EmployeeCount,
    decimal TotalGross,
    decimal TotalDiscounts,
    decimal TotalNet,
    decimal TotalFgtsEmployer,
    int EmployeesOnVacation,
    int NightShiftsInMonth,
    IReadOnlyList<PayrollDepartmentSummaryDto> ByDepartment);

public record PharmacyBillingEntryDto(
    Guid Id,
    Guid DispensingId,
    DateTime DispensedAt,
    string PatientName,
    string ProductName,
    decimal Quantity,
    PharmacyBillingPayerType PayerType,
    Guid? HealthInsuranceId,
    string? HealthInsuranceName,
    decimal UnitPrice,
    decimal TotalAmount,
    bool Paid,
    DateTime? PaidAt,
    Guid? FinancialAccountId,
    string? Notes);

public record CreatePharmacyBillingEntryRequest(
    Guid DispensingId,
    PharmacyBillingPayerType PayerType,
    Guid? HealthInsuranceId,
    decimal UnitPrice,
    bool Paid,
    string? Notes,
    bool CreateFinancialAccountWhenPaid = false);

public record BirthRegistrationDto(
    Guid Id,
    Guid MotherPatientId,
    string MotherName,
    string BabyName,
    DateTime BirthAt,
    decimal WeightKg,
    decimal HeightCm,
    string? Notes);

public record CreateBirthRegistrationRequest(
    Guid MotherPatientId,
    string BabyName,
    DateTime BirthAt,
    decimal WeightKg,
    decimal HeightCm,
    string? Notes);
