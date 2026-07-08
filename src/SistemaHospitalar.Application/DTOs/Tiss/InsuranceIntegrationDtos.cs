using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Tiss;

public record TussSearchResultDto(
    string TussCode,
    string Description,
    string Source,
    decimal? SuggestedPrice);

public record EligibilityCheckRequest(
    Guid PatientId,
    Guid HealthInsuranceId,
    string? CardNumber);

public record EligibilityCheckDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid HealthInsuranceId,
    string HealthInsuranceName,
    string CardNumber,
    EligibilityStatus Status,
    string? PlanName,
    string? CoverageSummary,
    DateTime? ValidUntil,
    string? ResponseMessage,
    DateTime CreatedAt);

public record CreateAuthorizationRequest(
    Guid PatientId,
    Guid HealthInsuranceId,
    InsuranceAuthorizationType AuthorizationType,
    string AuthorizationNumber,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    string? ProcedureSummary,
    Guid? TissGuideId,
    string? Notes);

public record RequestOnlineAuthorizationRequest(
    Guid PatientId,
    Guid HealthInsuranceId,
    InsuranceAuthorizationType AuthorizationType,
    string? ProcedureSummary,
    Guid? TissGuideId,
    string? Notes,
    DateTime? ValidFrom,
    DateTime? ValidUntil);

public record UpdateAuthorizationRequest(
    InsuranceAuthorizationStatus Status,
    string? AuthorizationNumber,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    string? ProcedureSummary,
    string? Notes);

public record InsuranceAuthorizationDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid HealthInsuranceId,
    string HealthInsuranceName,
    InsuranceAuthorizationType AuthorizationType,
    InsuranceAuthorizationStatus Status,
    string AuthorizationNumber,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    string? ProcedureSummary,
    Guid? TissGuideId,
    string? Notes,
    DateTime CreatedAt);

public record CreateTissBatchRequest(
    Guid HealthInsuranceId,
    string Competence,
    IReadOnlyList<Guid>? GuideIds);

public record TissBatchDto(
    Guid Id,
    string BatchNumber,
    Guid HealthInsuranceId,
    string HealthInsuranceName,
    string Competence,
    TissBatchStatus Status,
    string? ProtocolNumber,
    DateTime? SentAt,
    decimal TotalAmount,
    int GuideCount,
    DateTime CreatedAt);

public record TissBatchDetailDto(
    Guid Id,
    string BatchNumber,
    Guid HealthInsuranceId,
    string HealthInsuranceName,
    string Competence,
    TissBatchStatus Status,
    string? ProtocolNumber,
    DateTime? SentAt,
    decimal TotalAmount,
    int GuideCount,
    string? XmlContent,
    IReadOnlyList<TissGuideSummaryDto> Guides,
    DateTime CreatedAt);

public record TissGuideSummaryDto(
    Guid Id,
    string GuideNumber,
    string PatientName,
    decimal TotalAmount,
    TissGuideStatus Status);

public record TissConvenioDashboardDto(
    decimal TotalBilled,
    decimal TotalPaid,
    decimal TotalGlosaOpen,
    decimal GlosaRatePercent,
    int GuidesSentOver30Days,
    int GuidesSentOver60Days,
    IReadOnlyList<TissOperatorStatDto> ByOperator,
    IReadOnlyList<TissOperatorStatDto> GlosaByOperator);

public record TissOperatorStatDto(
    string OperatorName,
    int Count,
    decimal Amount);

public record SuggestedGuideItemsRequest(
    Guid PatientId,
    Guid? HospitalizationId,
    Guid? AppointmentId,
    TissGuideType? GuideType = null,
    Guid? SurgeryId = null);

public record GuidePrefillRequest(
    Guid PatientId,
    TissGuideType GuideType,
    Guid? HealthInsuranceId = null,
    bool IncludeOperatorData = true,
    bool RefreshOperatorData = false,
    Guid? AppointmentId = null,
    Guid? HospitalizationId = null,
    Guid? ChemotherapySessionId = null,
    Guid? SurgeryId = null,
    Guid? LabOrderId = null,
    Guid? ImagingStudyId = null);

public record ContestGlosaRequest(string? AnsGlosaCode, string ContestationNotes);

public record TissXmlValidationResultDto(
    bool IsValid,
    string? TissVersion,
    bool HashValid,
    string? ComputedHash,
    string? ProvidedHash,
    bool? SchemaValid,
    string? SchemaMessage,
    IReadOnlyList<string> Errors);

public record ValidateTissXmlRequest(string XmlContent);
