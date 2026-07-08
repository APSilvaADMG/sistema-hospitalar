using SistemaHospitalar.Application.DTOs.Tiss;

namespace SistemaHospitalar.Application.Interfaces;

public interface IInsuranceIntegrationService
{
    Task<IReadOnlyList<TussSearchResultDto>> SearchTussAsync(string? query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TissGuideItemRequest>> BuildSuggestedItemsAsync(
        SuggestedGuideItemsRequest request,
        CancellationToken cancellationToken = default);

    Task<TissGuidePrefillDto> GetGuidePrefillAsync(
        GuidePrefillRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProcedureLookupDto>> LookupProcedureAsync(
        string? query,
        CancellationToken cancellationToken = default);

    Task<BillingCatalogSummaryDto> GetBillingCatalogSummaryAsync(CancellationToken cancellationToken = default);

    Task<EligibilityCheckDto> CheckEligibilityAsync(
        EligibilityCheckRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EligibilityCheckDto>> GetEligibilityHistoryAsync(
        Guid? patientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InsuranceAuthorizationDto>> GetAuthorizationsAsync(
        Guid? patientId,
        Guid? healthInsuranceId,
        CancellationToken cancellationToken = default);

    Task<InsuranceAuthorizationDto> CreateAuthorizationAsync(
        CreateAuthorizationRequest request,
        CancellationToken cancellationToken = default);

    Task<InsuranceAuthorizationDto> RequestOnlineAuthorizationAsync(
        RequestOnlineAuthorizationRequest request,
        CancellationToken cancellationToken = default);

    Task<InsuranceAuthorizationDto?> UpdateAuthorizationAsync(
        Guid id,
        UpdateAuthorizationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TissBatchDto>> GetBatchesAsync(CancellationToken cancellationToken = default);

    Task<TissBatchDetailDto?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TissBatchDetailDto> CreateBatchAsync(
        CreateTissBatchRequest request,
        CancellationToken cancellationToken = default);

    Task<TissBatchDetailDto?> MarkBatchSentAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TissConvenioDashboardDto> GetConvenioDashboardAsync(CancellationToken cancellationToken = default);

    Task<TissXmlValidationResultDto> ValidateXmlAsync(string xmlContent, CancellationToken cancellationToken = default);

    Task<TissXmlValidationResultDto?> ValidateBatchXmlAsync(Guid batchId, CancellationToken cancellationToken = default);
}
