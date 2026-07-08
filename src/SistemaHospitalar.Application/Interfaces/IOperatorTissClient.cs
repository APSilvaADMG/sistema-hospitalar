using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Application.Interfaces;

public interface IOperatorTissClient
{
    Task<OperatorEligibilityResponse> CheckEligibilityAsync(
        HealthInsurance insurer,
        EligibilityCheckRequest request,
        PatientInsurance? patientInsurance,
        CancellationToken cancellationToken = default);

    Task<OperatorAuthorizationResponse> RequestAuthorizationAsync(
        HealthInsurance insurer,
        CreateAuthorizationRequest request,
        CancellationToken cancellationToken = default);

    Task<OperatorBatchSendResponse> SendBatchAsync(
        HealthInsurance insurer,
        TissBatch batch,
        IReadOnlyList<TissGuide> guides,
        CancellationToken cancellationToken = default);

    Task<OperatorDemonstrativoResponse> FetchDemonstrativoAsync(
        HealthInsurance insurer,
        TissBatch? batch,
        string competence,
        CancellationToken cancellationToken = default);
}

public record OperatorEligibilityResponse(
    bool IsEligible,
    string Message,
    string? PlanName,
    string? CoverageSummary,
    DateTime? ValidUntil,
    string RawJson);

public record OperatorAuthorizationResponse(
    bool Approved,
    string AuthorizationNumber,
    string Message,
    string RawJson);

public record OperatorBatchSendResponse(
    bool Success,
    string ProtocolNumber,
    string Message,
    string RawJson);

public record OperatorDemonstrativoResponse(
    bool Success,
    string DemonstrativoNumber,
    IReadOnlyList<OperatorDemonstrativoLine> Lines,
    string RawJson);

public record OperatorDemonstrativoLine(
    string GuideNumber,
    string? TussCode,
    decimal BilledAmount,
    decimal PaidAmount,
    decimal GlosaAmount,
    string? GlosaReason,
    string? AnsGlosaCode);
