using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Hospitalization;

public record HospitalizationHubFilterDto(
    DateTime? DateFrom,
    DateTime? DateTo,
    Guid? PatientId,
    Guid? WardId,
    Guid? ProfessionalId,
    WardCoverageModality? Modality,
    WardCategory? Category,
    HospitalizationStatus? Status,
    string? Search,
    string? GroupId,
    int Skip = 0,
    int Take = 50);

public record HospitalizationHubListItemDto(
    Guid Id,
    string ItemType,
    Guid PatientId,
    string PatientName,
    string? WardName,
    string? BedNumber,
    string? ProfessionalName,
    DateTime EventAt,
    int Status,
    string StatusLabel,
    string? ModalityLabel,
    string? Diagnosis,
    bool HasSusAih,
    int? DaysHospitalized);

public record HospitalizationHubListResultDto(
    int Total,
    IReadOnlyList<HospitalizationHubListItemDto> Items);

public record HospitalizationHubSliceDto(
    string Label,
    int Count);

public record HospitalizationHubDashboardDto(
    int ActiveCount,
    int DischargedInPeriod,
    int PendingRequests,
    int AvailableBeds,
    int OccupiedBeds,
    int BlockedBeds,
    double? AvgLengthOfStayDays,
    IReadOnlyList<HospitalizationHubSliceDto> ByWard,
    IReadOnlyList<HospitalizationHubSliceDto> ByModality,
    IReadOnlyList<HospitalizationHubSliceDto> ByProfessional);
