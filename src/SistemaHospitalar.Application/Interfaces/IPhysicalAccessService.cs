using SistemaHospitalar.Application.DTOs.PhysicalAccess;

namespace SistemaHospitalar.Application.Interfaces;

public interface IPhysicalAccessService
{
    Task<PhysicalAccessDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessZoneDto>> GetZonesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessTurnstileDto>> GetTurnstilesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessControlRecordDto>> GetAccessRecordsAsync(int? limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessCredentialDto>> GetCredentialsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FacialBiometricDto>> GetFacialEnrollmentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RegisteredVehicleDto>> GetVehiclesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LprReadEventDto>> GetLprEventsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KioskTicketDto>> GetKioskTicketsAsync(bool? pendingOnly, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessIntegrationProfileDto>> GetIntegrationProfilesAsync();
    Task<IReadOnlyList<EmployeeSectorAccessDto>> GetEmployeeAccessAsync(CancellationToken cancellationToken = default);

    Task<AppointmentQrDto?> GetAppointmentQrAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<TurnstileValidationResultDto> ValidateTurnstileAsync(TurnstileValidationRequest request, CancellationToken cancellationToken = default);
    Task<AccessCredentialDto> IssueCompanionCredentialAsync(IssueCompanionCredentialRequest request, CancellationToken cancellationToken = default);
    Task<FacialBiometricDto> EnrollFacialAsync(EnrollFacialRequest request, CancellationToken cancellationToken = default);
    Task<TurnstileValidationResultDto> ValidateFacialAccessAsync(FacialValidationRequest request, CancellationToken cancellationToken = default);
    Task<KioskCheckInResultDto> KioskCheckInAsync(KioskCheckInRequest request, CancellationToken cancellationToken = default);
    Task<KioskTicketDto> IssueKioskTicketAsync(IssueKioskTicketRequest request, CancellationToken cancellationToken = default);
    Task<RegisteredVehicleDto> RegisterVehicleAsync(RegisterVehicleRequest request, CancellationToken cancellationToken = default);
    Task<LprReadResultDto> ProcessLprReadAsync(LprReadRequest request, CancellationToken cancellationToken = default);
}
