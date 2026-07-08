using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Guides;

public record GuidesHubFilterDto(
    DateTime? DateFrom,
    DateTime? DateTo,
    Guid? PatientId,
    Guid? HealthInsuranceId,
    Guid? ProfessionalId,
    Guid? SpecialtyId,
    string? ProcedureSearch,
    string? GuideNumber,
    TissGuideStatus? Status,
    TissGuideType? GuideType,
    string? GroupId,
    string? ServiceUnit,
    Guid? ServiceUnitId,
    int Skip = 0,
    int Take = 50);

public record GuideHubListItemDto(
    Guid Id,
    string GuideNumber,
    string PatientName,
    string HealthInsuranceName,
    string? RequestingProfessionalName,
    string? SpecialtyName,
    string? ProcedureSummary,
    string? Cid10Code,
    DateTime CreatedAt,
    DateTime? AuthorizedAt,
    int Status,
    string StatusLabel,
    int GuideType,
    string GuideTypeLabel,
    string ServiceUnit,
    decimal TotalAmount,
    string Source);

public record GuidesHubListResultDto(
    int Total,
    IReadOnlyList<GuideHubListItemDto> Items);

public record GuidesHubProductionSliceDto(
    string Label,
    int Count,
    decimal Amount);

public record GuidesHubDashboardDto(
    int IssuedCount,
    int AuthorizedCount,
    int PendingCount,
    int BilledCount,
    int GlosaCount,
    double? AvgAuthorizationHours,
    IReadOnlyList<GuidesHubProductionSliceDto> ByInsurance,
    IReadOnlyList<GuidesHubProductionSliceDto> ByProfessional,
    IReadOnlyList<GuidesHubProductionSliceDto> BySpecialty);

public record GuideHistoryEntryDto(
    DateTime OccurredAt,
    string Action,
    string? UserEmail,
    string Details,
    string Source);
