using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[ApiController]
[Route("api/connect")]
[Authorize]
public class ConnectController(IConnectService connectService) : ControllerBase
{
    [HttpGet("dashboard")]
    [RequireAnyPermission(PermissionCodes.ConnectRead, PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await connectService.GetDashboardAsync(cancellationToken));

    [HttpGet("conversations")]
    [RequireAnyPermission(PermissionCodes.ConnectRead, PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int limit = 50,
        [FromQuery] ConnectBotStep? botStep = null,
        [FromQuery] ConnectInboxQueue? queue = null,
        [FromQuery] bool awaitingHumanOnly = false,
        CancellationToken cancellationToken = default)
        => Ok(await connectService.GetConversationsAsync(
            new ConnectConversationQuery(limit, botStep, queue, awaitingHumanOnly),
            cancellationToken));

    [HttpGet("conversations/{id:guid}")]
    [RequireAnyPermission(PermissionCodes.ConnectRead, PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken cancellationToken)
    {
        var detail = await connectService.GetConversationAsync(id, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("conversations/{id:guid}/reply")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ConnectReplyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var message = await connectService.ReplyAsync(id, request, GetUserId(), cancellationToken);
            return Ok(message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("conversations/{id:guid}/assign")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> Assign(Guid id, [FromBody] ConnectAssignRequest request, CancellationToken cancellationToken)
    {
        var conversation = await connectService.AssignConversationAsync(id, request, cancellationToken);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpPost("conversations/{id:guid}/resolve")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await connectService.ResolveConversationAsync(id, cancellationToken);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpGet("inbox/summary")]
    [RequireAnyPermission(PermissionCodes.ConnectRead, PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    public async Task<IActionResult> GetInboxSummary(CancellationToken cancellationToken)
        => Ok(await connectService.GetInboxSummaryAsync(cancellationToken));

    [HttpGet("waitlist")]
    [RequireAnyPermission(PermissionCodes.ConnectRead, PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    public async Task<IActionResult> GetWaitlist(CancellationToken cancellationToken)
        => Ok(await connectService.GetWaitlistAsync(cancellationToken));

    [HttpPost("waitlist")]
    [RequireAnyPermission(PermissionCodes.ConnectWrite, PermissionCodes.PatientsCreate)]
    public async Task<IActionResult> JoinWaitlist([FromBody] JoinWaitlistRequest request, CancellationToken cancellationToken)
        => Ok(await connectService.JoinWaitlistAsync(request, cancellationToken));

    [HttpGet("knowledge")]
    [RequireAnyPermission(PermissionCodes.ConnectRead, PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    public async Task<IActionResult> GetKnowledge(CancellationToken cancellationToken)
        => Ok(await connectService.GetKnowledgeArticlesAsync(cancellationToken));

    [HttpGet("satisfaction")]
    [RequireAnyPermission(PermissionCodes.ConnectRead, PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    public async Task<IActionResult> GetSatisfaction(CancellationToken cancellationToken)
        => Ok(await connectService.GetSatisfactionStatsAsync(cancellationToken));

    [HttpPost("simulate")]
    [RequireAnyPermission(PermissionCodes.ConnectWrite, PermissionCodes.PatientsRead, PermissionCodes.PepRead)]
    public async Task<IActionResult> Simulate([FromBody] SimulateInboundRequest request, CancellationToken cancellationToken)
        => Ok(await connectService.SimulateInboundAsync(request, cancellationToken));

    [HttpPost("block-schedule")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> BlockSchedule([FromBody] BlockProfessionalScheduleRequest request, CancellationToken cancellationToken)
        => Ok(await connectService.BlockProfessionalScheduleAsync(request, cancellationToken));

    [HttpGet("integration-status")]
    [RequireAnyPermission(PermissionCodes.ConnectRead, PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    public async Task<IActionResult> GetIntegrationStatus(CancellationToken cancellationToken)
        => Ok(await connectService.GetIntegrationStatusAsync(cancellationToken));

    private Guid? GetUserId()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
