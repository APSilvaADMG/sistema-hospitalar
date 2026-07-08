using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Hospitalization;

public record WardDto(
    Guid Id,
    string Name,
    string? Code,
    string? Floor,
    string? Description,
    WardCoverageModality CoverageModality,
    WardCategory Category,
    int TotalBeds,
    int AvailableBeds,
    int OccupiedBeds,
    int BlockedBeds);

public record BedDto(
    Guid Id,
    Guid WardId,
    string WardName,
    string? WardCode,
    WardCoverageModality WardCoverageModality,
    WardCategory WardCategory,
    string BedNumber,
    BedStatus Status,
    string? StatusReason = null,
    DateTime? BlockedUntil = null,
    Guid? OccupantPatientId = null,
    string? OccupantPatientName = null,
    string? OccupantProfessionalName = null,
    DateTime? OccupantAdmittedAt = null);

public record CreateWardRequest(
    string Name,
    string? Code,
    string? Floor,
    string? Description,
    WardCoverageModality CoverageModality,
    WardCategory Category);

public record UpdateWardRequest(
    string Name,
    string? Code,
    string? Floor,
    string? Description,
    WardCoverageModality CoverageModality,
    WardCategory Category);

public record CreateBedRequest(
    Guid WardId,
    string BedNumber);

public record UpdateBedRequest(
    string BedNumber);

public record UpdateBedStatusRequest(
    BedStatus Status,
    string? Reason,
    DateTime? BlockedUntil);

public record HospitalizationSusDataDto(
    string? AihNumber,
    string? SusCompetence,
    string? PrimaryCid10Code,
    string? SecondaryCid10Code,
    string? PrimarySigtapProcedureCode,
    string? SecondarySigtapProcedureCode,
    SusHospitalizationCharacter? SusCharacter,
    SusHospitalizationModality? SusModality,
    string? CnesCode,
    string? SusAuthorizationNumber,
    DateTime? AihExportedAt);

public record UpdateHospitalizationSusDataRequest(
    string? AihNumber,
    string? SusCompetence,
    string? PrimaryCid10Code,
    string? SecondaryCid10Code,
    string? PrimarySigtapProcedureCode,
    string? SecondarySigtapProcedureCode,
    SusHospitalizationCharacter? SusCharacter,
    SusHospitalizationModality? SusModality,
    string? CnesCode,
    string? SusAuthorizationNumber);

public record HospitalizationSusDataInput(
    string? PrimaryCid10Code,
    string? SecondaryCid10Code,
    string? PrimarySigtapProcedureCode,
    string? SecondarySigtapProcedureCode,
    SusHospitalizationCharacter? SusCharacter,
    SusHospitalizationModality? SusModality,
    string? CnesCode,
    string? SusAuthorizationNumber);

public enum HospitalizationListScope
{
    Active = 0,
    Discharged = 1,
    Deceased = 2,
    All = 3
}

public record HospitalizationDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    bool PatientIsDeceased,
    string? PatientCns,
    Guid BedId,
    string BedNumber,
    string WardName,
    string? WardCode,
    WardCoverageModality WardCoverageModality,
    WardCategory WardCategory,
    Guid ProfessionalId,
    string ProfessionalName,
    DateTime AdmittedAt,
    DateTime? DischargedAt,
    HospitalizationStatus Status,
    string Reason,
    string? Diagnosis,
    DateTime? BillingAccountClosedAt,
    HospitalizationSusDataDto? SusData);

public record HospitalizationRequestDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid RequestingProfessionalId,
    string RequestingProfessionalName,
    Guid? PreferredWardId,
    string? PreferredWardName,
    WardCategory? PreferredWardCategory,
    string Reason,
    string? Diagnosis,
    string? Cid10Code,
    string? Notes,
    HospitalizationRequestPriority Priority,
    HospitalizationRequestStatus Status,
    DateTime RequestedAt,
    Guid? ReviewedByProfessionalId,
    string? ReviewedByProfessionalName,
    DateTime? ReviewedAt,
    string? ReviewNotes,
    Guid? HospitalizationId);

public record CreateHospitalizationRequestRequest(
    Guid PatientId,
    Guid RequestingProfessionalId,
    string Reason,
    string? Diagnosis,
    string? Cid10Code,
    string? Notes,
    Guid? PreferredWardId,
    WardCategory? PreferredWardCategory,
    HospitalizationRequestPriority Priority,
    Guid? AiTriageLogId);

public record ReviewHospitalizationRequestRequest(
    bool Approve,
    Guid ReviewedByProfessionalId,
    string? ReviewNotes);

public record AdmitFromHospitalizationRequestRequest(
    Guid BedId,
    Guid ProfessionalId,
    string? Notes,
    HospitalizationSusDataInput? SusData = null);

public record AdmitPatientRequest(
    Guid PatientId,
    Guid BedId,
    Guid ProfessionalId,
    string Reason,
    string? Diagnosis,
    string? Notes,
    Guid? AiTriageLogId = null,
    Guid? HospitalizationRequestId = null,
    HospitalizationSusDataInput? SusData = null);

public record DischargePatientRequest(string? Notes);

public record RegisterPatientDeathRequest(string? Notes, string? PrimaryCid10Code);

public record TransferBedRequest(
    Guid TargetBedId,
    Guid? ProfessionalId,
    string? Reason);

public record HospitalizationSnippetDto(
    Guid Id,
    string Text,
    int UsageCount);

public record RegisterHospitalizationSnippetRequest(
    HospitalizationSnippetType Type,
    string Text);

public record BedTransferDto(
    Guid Id,
    Guid HospitalizationId,
    string PatientName,
    string FromWardName,
    string FromBedNumber,
    string ToWardName,
    string ToBedNumber,
    string? ProfessionalName,
    DateTime TransferredAt,
    string? Reason);

public record ReserveBedRequest(
    Guid PatientId,
    string? Reason,
    DateTime? Until);

public record BlockBedRequest(
    string Reason,
    DateTime? Until);

public record ReleaseBedRequest(string? Reason);

public record BedEventDto(
    Guid Id,
    Guid BedId,
    string BedNumber,
    string WardName,
    BedEventType EventType,
    Guid? PatientId,
    string? PatientName,
    Guid? HospitalizationId,
    string? Reason,
    DateTime StartAt,
    DateTime? EndAt);
