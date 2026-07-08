using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.PhysicalAccess;

public record AccessZoneDto(Guid Id, string Code, string Name, string? Building, string? Floor, bool RequiresAuthorization);

public record AccessTurnstileDto(Guid Id, string Code, string Name, Guid? ZoneId, string? ZoneName, string? IntegrationVendor, bool IsEntry);

public record PhysicalAccessDashboardDto(
    int PeopleInsideEstimate,
    int AccessGrantedToday,
    int AccessDeniedToday,
    int ActiveCompanions,
    int VehiclesInside,
    int FacialEnrollments,
    IReadOnlyList<AccessControlRecordDto> RecentAccess,
    IReadOnlyList<LprReadEventDto> RecentLpr);

public record AccessControlRecordDto(
    Guid Id,
    AccessPersonType PersonType,
    string PersonName,
    AccessMethod Method,
    AccessDirection Direction,
    AccessValidationResult Result,
    string? Location,
    string? Details,
    DateTime OccurredAt);

public record AccessCredentialDto(
    Guid Id,
    AccessPersonType PersonType,
    string HolderName,
    AccessCredentialType CredentialType,
    AccessCredentialStatus Status,
    string Token,
    string? ZoneName,
    DateTime? ValidUntil);

public record FacialBiometricDto(
    Guid Id,
    AccessPersonType PersonType,
    string PersonName,
    FacialBiometricStatus Status,
    DateTime EnrolledAt,
    bool HasPhoto);

public record RegisteredVehicleDto(
    Guid Id,
    string Plate,
    string? Model,
    string? Color,
    VehicleOwnerCategory OwnerCategory,
    string OwnerName,
    bool ParkingExempt);

public record LprReadEventDto(
    Guid Id,
    string Plate,
    string CameraLocation,
    AccessDirection Direction,
    bool GateOpened,
    string? OwnerName,
    VehicleOwnerCategory? OwnerCategory,
    DateTime ReadAt);

public record KioskTicketDto(
    Guid Id,
    KioskTicketType TicketType,
    string TicketNumber,
    string? PatientName,
    string? Sector,
    DateTime IssuedAt,
    bool Called);

public record AccessIntegrationProfileDto(
    string Vendor,
    AccessIntegrationCategory Category,
    string Description,
    bool MockEnabled,
    string? Endpoint);

public record AppointmentQrDto(Guid AppointmentId, string QrPayload, string PatientName, DateTime ScheduledAt);

public record TurnstileValidationRequest(
    string TurnstileCode,
    AccessMethod Method,
    string Payload,
    AccessDirection Direction = AccessDirection.Entry);

public record TurnstileValidationResultDto(
    bool Granted,
    AccessValidationResult Result,
    string Message,
    string? PersonName,
    AccessPersonType? PersonType,
    Guid? RecordId);

public record IssueCompanionCredentialRequest(
    Guid PatientId,
    string CompanionName,
    string? DocumentNumber,
    AccessCredentialType CredentialType,
    Guid? AllowedZoneId,
    TimeOnly? VisitStartTime,
    TimeOnly? VisitEndTime,
    DateTime? ValidUntil);

public record EnrollFacialRequest(
    AccessPersonType PersonType,
    string PersonName,
    Guid? PatientId,
    Guid? EmployeeId,
    Guid? ProfessionalId,
    string? PhotoData,
    string TemplatePayload);

public record FacialValidationRequest(
    string TurnstileCode,
    Guid? PersonId,
    AccessPersonType? PersonType,
    string? TemplatePayload);

public record KioskCheckInRequest(
    string? Cpf,
    string? QrPayload,
    Guid? FacialTemplateId);

public record KioskCheckInResultDto(
    bool Success,
    string Message,
    Guid? AppointmentId,
    string? PatientName,
    KioskTicketDto? Ticket);

public record IssueKioskTicketRequest(KioskTicketType TicketType, Guid? PatientId, string? PatientName, string? Sector);

public record RegisterVehicleRequest(
    string Plate,
    string? Model,
    string? Color,
    VehicleOwnerCategory OwnerCategory,
    string OwnerName,
    Guid? PatientId,
    Guid? EmployeeId,
    bool ParkingExempt);

public record LprReadRequest(string Plate, string CameraLocation, AccessDirection Direction);

public record LprReadResultDto(
    bool GateOpened,
    string Message,
    RegisteredVehicleDto? Vehicle,
    LprReadEventDto Event);

public record EmployeeSectorAccessDto(
    Guid EmployeeId,
    string EmployeeName,
    string Department,
    string? AllowedZone,
    bool OnShift,
    DateTime? LastAccess);
