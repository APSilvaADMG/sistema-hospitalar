using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Ai;

public record TriageRequest(
    string Symptoms,
    Guid? PatientId,
    string? DocumentNumber = null,
    string? SusCardNumber = null,
    string? HealthInsuranceName = null,
    int? SystolicBp = null,
    int? DiastolicBp = null,
    decimal? TemperatureC = null,
    int? HeartRateBpm = null,
    int? OxygenSaturationPct = null,
    int? PainLevel = null,
    string? HealthHistory = null);

public record TriageResponse(
    Guid TriageLogId,
    TriageUrgency Urgency,
    string UrgencyLabel,
    string ManchesterColor,
    string ManchesterColorHex,
    int MaxWaitMinutes,
    string Referral,
    string ReferralLabel,
    string RecommendedSpecialty,
    string? SuggestedCid10,
    string? SuggestedCid10Description,
    string Guidance,
    IReadOnlyList<Cid10SuggestionDto> RelatedCodes);

public record Cid10SuggestionRequest(string Text, int MaxResults = 5);

public record Cid10SuggestionDto(string Code, string Description, string? Category, int Score);

public record PrescriptionSafetyRequest(Guid PatientId, string PrescriptionContent);

public record AiTriageLogDto(
    Guid Id,
    string? PatientName,
    string Symptoms,
    TriageUrgency Urgency,
    string UrgencyLabel,
    string ManchesterColor,
    int MaxWaitMinutes,
    string RecommendedSpecialty,
    string? SuggestedCid10,
    DateTime CreatedAt);

public record TriageAdmissionSuggestionDto(
    Guid TriageLogId,
    string Reason,
    string? Diagnosis,
    TriageUrgency Urgency,
    string UrgencyLabel,
    string ManchesterColor,
    string RecommendedSpecialty,
    string? SuggestedCid10,
    string? SuggestedCid10Description,
    DateTime CreatedAt);

public enum AiInsightType
{
    Outbreak = 1,
    RecurrentPatient = 2,
    TriageOperational = 3,
    HospitalDashboard = 4,
}

public record AiInsightIndicatorDto(string Label, string Value, string? Severity = null);

public record AiInsightReportDto(
    Guid Id,
    AiInsightType Type,
    string Title,
    string Summary,
    string RiskLevel,
    IReadOnlyList<AiInsightIndicatorDto> Indicators,
    string Markdown,
    DateTime CreatedAt,
    Guid? PatientId,
    string? PatientName,
    bool GroqEnriched = false,
    string? AiModel = null);
