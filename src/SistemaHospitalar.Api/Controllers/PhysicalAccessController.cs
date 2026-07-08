using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.PhysicalAccess;
using SistemaHospitalar.Application.DTOs.TvSignage;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
[ApiController]
[Route("api/physical-access")]
public class PhysicalAccessController(
    IPhysicalAccessService physicalAccessService,
    ITvSignageService tvSignageService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await physicalAccessService.GetDashboardAsync(cancellationToken));

    [HttpGet("zones")]
    public async Task<IActionResult> GetZones(CancellationToken cancellationToken)
        => Ok(await physicalAccessService.GetZonesAsync(cancellationToken));

    [HttpGet("turnstiles")]
    public async Task<IActionResult> GetTurnstiles(CancellationToken cancellationToken)
        => Ok(await physicalAccessService.GetTurnstilesAsync(cancellationToken));

    [HttpGet("records")]
    public async Task<IActionResult> GetRecords([FromQuery] int? limit, CancellationToken cancellationToken)
        => Ok(await physicalAccessService.GetAccessRecordsAsync(limit, cancellationToken));

    [HttpGet("credentials")]
    public async Task<IActionResult> GetCredentials(CancellationToken cancellationToken)
        => Ok(await physicalAccessService.GetCredentialsAsync(cancellationToken));

    [HttpGet("facial")]
    public async Task<IActionResult> GetFacial(CancellationToken cancellationToken)
        => Ok(await physicalAccessService.GetFacialEnrollmentsAsync(cancellationToken));

    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehicles(CancellationToken cancellationToken)
        => Ok(await physicalAccessService.GetVehiclesAsync(cancellationToken));

    [HttpGet("lpr")]
    public async Task<IActionResult> GetLpr(CancellationToken cancellationToken)
        => Ok(await physicalAccessService.GetLprEventsAsync(cancellationToken));

    [HttpGet("kiosk/tickets")]
    public async Task<IActionResult> GetKioskTickets([FromQuery] bool? pendingOnly, CancellationToken cancellationToken)
        => Ok(await physicalAccessService.GetKioskTicketsAsync(pendingOnly, cancellationToken));

    [HttpGet("integrations")]
    public async Task<IActionResult> GetIntegrations()
        => Ok(await physicalAccessService.GetIntegrationProfilesAsync());

    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployeeAccess(CancellationToken cancellationToken)
        => Ok(await physicalAccessService.GetEmployeeAccessAsync(cancellationToken));

    [HttpGet("appointments/{id:guid}/qr")]
    public async Task<IActionResult> GetAppointmentQr(Guid id, CancellationToken cancellationToken)
    {
        var result = await physicalAccessService.GetAppointmentQrAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("turnstile/validate")]
    public async Task<IActionResult> ValidateTurnstile([FromBody] TurnstileValidationRequest request, CancellationToken cancellationToken)
        => Ok(await physicalAccessService.ValidateTurnstileAsync(request, cancellationToken));

    [HttpPost("companions/credential")]
    public async Task<IActionResult> IssueCompanionCredential([FromBody] IssueCompanionCredentialRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await physicalAccessService.IssueCompanionCredentialAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("facial/enroll")]
    public async Task<IActionResult> EnrollFacial([FromBody] EnrollFacialRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await physicalAccessService.EnrollFacialAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("facial/validate")]
    public async Task<IActionResult> ValidateFacial([FromBody] FacialValidationRequest request, CancellationToken cancellationToken)
        => Ok(await physicalAccessService.ValidateFacialAccessAsync(request, cancellationToken));

    [HttpPost("kiosk/check-in")]
    public async Task<IActionResult> KioskCheckIn([FromBody] KioskCheckInRequest request, CancellationToken cancellationToken)
        => Ok(await physicalAccessService.KioskCheckInAsync(request, cancellationToken));

    [HttpPost("kiosk/tickets")]
    public async Task<IActionResult> IssueKioskTicket([FromBody] IssueKioskTicketRequest request, CancellationToken cancellationToken)
        => Ok(await physicalAccessService.IssueKioskTicketAsync(request, cancellationToken));

    [HttpPost("kiosk/tickets/{id:guid}/call")]
    [RequireAnyPermission(PermissionCodes.ConnectWrite, PermissionCodes.ReportsRead)]
    public async Task<IActionResult> CallKioskTicketOnTv(Guid id, [FromBody] CallKioskTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await tvSignageService.CallKioskTicketAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("vehicles")]
    public async Task<IActionResult> RegisterVehicle([FromBody] RegisterVehicleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await physicalAccessService.RegisterVehicleAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("lpr/read")]
    public async Task<IActionResult> ProcessLpr([FromBody] LprReadRequest request, CancellationToken cancellationToken)
        => Ok(await physicalAccessService.ProcessLprReadAsync(request, cancellationToken));
}
