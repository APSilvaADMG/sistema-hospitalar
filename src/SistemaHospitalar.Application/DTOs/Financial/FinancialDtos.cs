using SistemaHospitalar.Domain.Enums;



namespace SistemaHospitalar.Application.DTOs.Financial;



public record FinancialPaymentInstallmentDto(

    int InstallmentNumber,

    int InstallmentCount,

    decimal Amount,

    DateTime DueDate,

    Guid? FinancialAccountId);



public record FinancialPaymentDto(

    Guid Id,

    Guid FinancialAccountId,

    decimal Amount,

    PaymentMethod Method,

    DateTime PaidAt,

    string? Notes,

    DateTime CreatedAt,

    int? InstallmentCount,

    IReadOnlyList<FinancialPaymentInstallmentDto> Installments);



public record FinancialAccountLineItemDto(
    Guid Id,
    string Description,
    int Quantity,
    decimal UnitAmount,
    decimal TotalAmount,
    string? Notes);

public record FinancialAccountDto(

    Guid Id,

    FinancialAccountDirection Direction,

    Guid? PatientId,

    string? PatientName,

    Guid? SupplierId,

    string? SupplierName,

    string? CounterpartyName,

    string CounterpartyDisplay,

    Guid? AppointmentId,

    Guid? HospitalizationId,

    FinancialAccountCategory Category,

    string Description,

    string? Notes,

    string? InvoiceNumber,

    decimal Amount,

    decimal PaidAmount,

    decimal Balance,

    FinancialAccountStatus Status,

    DateTime? DueDate,

    DateTime? PaidAt,

    PaymentMethod? LastPaymentMethod,

    PaymentMethod? ExpectedPaymentMethod,

    int PaymentCount,

    DateTime CreatedAt,

    Guid? ParentFinancialAccountId,

    int? InstallmentNumber,

    int? InstallmentCount,

    IReadOnlyList<FinancialAccountLineItemDto> LineItems);



public record FinancialAccountLineItemInput(
    string Description,
    int Quantity,
    decimal UnitAmount,
    string? Notes = null);



public record FinancialSummaryDto(

    decimal ReceivableOpen,

    decimal PayableOpen,

    decimal TotalReceived,

    decimal TotalPaidOut,

    decimal ReceivedThisMonth,

    decimal PaidOutThisMonth,

    int OpenProposalsCount,

    decimal OpenProposalsBalance,

    int OpenHonorariosCount,

    decimal OpenHonorariosBalance);



public record CreateFinancialAccountRequest(

    FinancialAccountDirection Direction,

    Guid? PatientId,

    Guid? SupplierId,

    string? CounterpartyName,

    Guid? AppointmentId,

    Guid? HospitalizationId,

    FinancialAccountCategory Category,

    string Description,

    decimal Amount,

    DateTime? DueDate,

    string? Notes,

    PaymentMethod? ExpectedPaymentMethod,

    string? InvoiceNumber = null,

    int? InstallmentCount = null,

    IReadOnlyList<FinancialAccountLineItemInput>? LineItems = null);



public record RegisterPaymentRequest(

    decimal Amount,

    PaymentMethod Method,

    DateTime? PaidAt,

    string? Notes,

    Guid? PixChargeId = null,

    IReadOnlyList<PaymentInstallmentInput>? Installments = null);



public record PaymentInstallmentInput(

    int InstallmentNumber,

    decimal Amount,

    DateTime DueDate);



public record FinancialAccountSourceOptionDto(

    string SourceType,

    Guid SourceId,

    string Label,

    string Detail,

    decimal SuggestedAmount,

    string SuggestedDescription,

    FinancialAccountCategory SuggestedCategory,

    bool AlreadyBilled);



public record FinancialAccountCategoryPresetDto(

    FinancialAccountCategory Category,

    string Label,

    decimal SuggestedAmount,

    string DescriptionTemplate,

    int SuggestedDueDays);



public record FinancialAccountCreateSuggestionsDto(

    Guid PatientId,

    string PatientName,

    string Cpf,

    string? Phone,

    string? InsuranceName,

    int PaymentModality,

    int SuggestedDueDays,

    decimal OutstandingBalance,

    IReadOnlyList<FinancialAccountSourceOptionDto> SourceOptions,

    IReadOnlyList<FinancialAccountCategoryPresetDto> CategoryPresets);



public record PayableCategoryPresetDto(

    FinancialAccountCategory Category,

    string Label,

    decimal SuggestedAmount,

    string DescriptionTemplate,

    int SuggestedDueDays);


