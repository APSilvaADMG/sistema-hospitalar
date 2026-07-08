using SistemaHospitalar.Application.Common;
using SistemaHospitalar.Application.DTOs.Financial;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IFinancialAccountService
{
    Task<PagedResult<FinancialAccountDto>> SearchAsync(
        FinancialAccountStatus? status,
        FinancialAccountDirection? direction,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialAccountDto>> GetByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<FinancialAccountDto> CreateAsync(
        CreateFinancialAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<FinancialAccountDto?> RegisterPaymentAsync(
        Guid id,
        RegisterPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialPaymentDto>> GetPaymentsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<FinancialSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task CreateFromAppointmentAsync(
        Guid appointmentId,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialAccountDto>> GetOutstandingByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<FinancialAccountCreateSuggestionsDto> GetCreateSuggestionsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    IReadOnlyList<PayableCategoryPresetDto> GetPayableCategoryPresets();

    Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FinancialAccountDto?> ConvertProposalToBillingAsync(
        Guid proposalId,
        CancellationToken cancellationToken = default);
}
