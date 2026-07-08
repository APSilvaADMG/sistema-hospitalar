using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Tiss;

public record TissGuideItemDto(
    Guid Id,
    string TussCode,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal Total,
    TissPriceTableSource? PriceTableSource,
    string? Cid10Code,
    string? RelatedTussCode,
    bool IsAudited);

public record TissGlosaDto(
    Guid Id,
    Guid? TissGuideItemId,
    string Reason,
    string? AnsGlosaCode,
    decimal GlosaAmount,
    bool IsResolved,
    GlosaContestationStatus ContestationStatus,
    string? ContestationNotes,
    string? ItemDescription);

public record TissGuideDto(
    Guid Id,
    string GuideNumber,
    Guid PatientId,
    string PatientName,
    Guid HealthInsuranceId,
    string HealthInsuranceName,
    Guid? AppointmentId,
    Guid? HospitalizationId,
    TissGuideType GuideType,
    TissGuideStatus Status,
    decimal TotalAmount,
    DateTime? SentAt,
    DateTime? AccountClosedAt,
    string? Notes,
    string? BeneficiaryCardNumber,
    string? BeneficiaryPlanName,
    string? BeneficiaryCns,
    string? AuthorizationPassword,
    TissGuideClinicalDto Clinical,
    Guid? ServiceUnitId,
    string? ServiceUnitName,
    DateTime CreatedAt,
    IReadOnlyList<TissGuideItemDto> Items,
    IReadOnlyList<TissGlosaDto> Glosas);

public record UpdateTissGuideItemRequest(
    Guid? Id,
    string TussCode,
    string Description,
    int Quantity,
    decimal UnitPrice,
    TissPriceTableSource? PriceTableSource = null,
    string? Cid10Code = null,
    string? RelatedTussCode = null);

public record CreateTissGuideRequest(
    Guid PatientId,
    Guid HealthInsuranceId,
    Guid? AppointmentId,
    Guid? HospitalizationId,
    TissGuideType GuideType,
    IReadOnlyList<TissGuideItemRequest> Items,
    string? Notes,
    TissGuideClinicalRequest? Clinical = null,
    string? ClientRequestId = null,
    Guid? ServiceUnitId = null);

public record UpdateTissGuideRequest(
    Guid HealthInsuranceId,
    Guid? AppointmentId,
    Guid? HospitalizationId,
    TissGuideType GuideType,
    string? Notes,
    TissGuideClinicalRequest? Clinical,
    IReadOnlyList<UpdateTissGuideItemRequest> Items,
    Guid? ServiceUnitId = null);

public record RegisterGlosaRequest(
    Guid? TissGuideItemId,
    string Reason,
    string? AnsGlosaCode,
    decimal GlosaAmount);

public record UpdateGlosaRequest(
    Guid? TissGuideItemId,
    string Reason,
    string? AnsGlosaCode,
    decimal GlosaAmount);
