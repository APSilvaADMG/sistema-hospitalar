using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Tiss;

public record TissGuideClinicalDto(
    string? Cid10Code,
    string? Cid10Secondary,
    string? ClinicalJustification,
    TissServiceCharacter? ServiceCharacter,
    TissAccidentIndicator? AccidentIndicator,
    Guid? RequestingProfessionalId,
    string? RequestingProfessionalName,
    string? RequestingProfessionalCrm,
    Guid? ExecutingProfessionalId,
    string? ExecutingProfessionalName,
    string? ExecutingProfessionalCrm,
    DateTime? AdmissionDate,
    DateTime? DischargeDate,
    string? RequestedBedType,
    Guid? ParentGuideId,
    TissProfessionalRole? ProfessionalRole,
    decimal? ParticipationPercent,
    Guid? SurgeryId);

public record TissGuideClinicalRequest(
    string? Cid10Code,
    string? Cid10Secondary,
    string? ClinicalJustification,
    TissServiceCharacter? ServiceCharacter,
    TissAccidentIndicator? AccidentIndicator,
    Guid? RequestingProfessionalId,
    string? RequestingProfessionalName,
    string? RequestingProfessionalCrm,
    Guid? ExecutingProfessionalId,
    string? ExecutingProfessionalName,
    string? ExecutingProfessionalCrm,
    DateTime? AdmissionDate,
    DateTime? DischargeDate,
    string? RequestedBedType,
    Guid? ParentGuideId,
    TissProfessionalRole? ProfessionalRole,
    decimal? ParticipationPercent,
    Guid? SurgeryId);

public record TissGuideItemRequest(
    string TussCode,
    string Description,
    int Quantity,
    decimal UnitPrice,
    TissPriceTableSource? PriceTableSource = null,
    string? Cid10Code = null,
    string? RelatedTussCode = null);

public record TissGuidePrefillDto(
    Guid? HealthInsuranceId,
    string? HealthInsuranceName,
    string? BeneficiaryCardNumber,
    string? BeneficiaryPlanName,
    string? BeneficiaryCns,
    string? BeneficiaryAccommodation,
    string? AuthorizationPassword,
    Guid? AppointmentId,
    Guid? HospitalizationId,
    Guid? SurgeryId,
    string? Cid10Code,
    string? Cid10Description,
    Guid? RequestingProfessionalId,
    string? RequestingProfessionalName,
    string? RequestingProfessionalCrm,
    Guid? ExecutingProfessionalId,
    string? ExecutingProfessionalName,
    DateTime? AdmissionDate,
    DateTime? DischargeDate,
    string? RequestedBedType,
    TissGuideType? SuggestedGuideType,
    IReadOnlyList<TissGuideItemRequest> SuggestedItems,
    EligibilityStatus? OperatorEligibilityStatus = null,
    DateTime? OperatorEligibilityCheckedAt = null,
    DateTime? CardValidUntil = null,
    string? OperatorMessage = null,
    string? OperatorCoverageSummary = null,
    string? OperatorDataSource = null);

public record ProcedureLookupDto(
    string Code,
    string Description,
    string Source,
    decimal? ReferencePrice,
    TissGuideType? SuggestedGuideType,
    TussTableType? TussTableType);

public record BillingCatalogSummaryDto(
    int TussCount,
    int CbhpmCount,
    int BrasindiceCount,
    int SimproCount,
    int Cid10Count);
