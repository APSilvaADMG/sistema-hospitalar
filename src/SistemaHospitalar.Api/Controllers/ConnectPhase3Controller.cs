using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[ApiController]
[Route("api/connect")]
[Authorize]
public class ConnectPhase3Controller(
    IConnectCalendarService calendarService,
    IConnectContextService contextService,
    IConnectAiAssistantService aiService) : ControllerBase
{
    [HttpGet("calendar")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> ListCalendar(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string scope = "all",
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await calendarService.ListByRangeAsync(userId.Value, from, to, scope, cancellationToken));
    }

    [HttpGet("calendar/{id:guid}")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> GetCalendarEvent(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var item = await calendarService.GetAsync(userId.Value, id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("calendar")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> CreateCalendarEvent(
        [FromBody] CreateConnectCalendarEventRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await calendarService.CreateAsync(userId.Value, request, cancellationToken));
    }

    [HttpPut("calendar/{id:guid}")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> UpdateCalendarEvent(
        Guid id,
        [FromBody] UpdateConnectCalendarEventRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var item = await calendarService.UpdateAsync(userId.Value, id, request, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("calendar/{id:guid}")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> DeleteCalendarEvent(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return await calendarService.DeleteAsync(userId.Value, id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("calendar/{id:guid}/respond")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> RespondCalendarEvent(
        Guid id,
        [FromBody] RespondCalendarEventRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var item = await calendarService.RespondAsync(userId.Value, id, request, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("context/patient/{patientId:guid}/messages")]
    [RequireAnyPermission(PermissionCodes.ConnectRead, PermissionCodes.PatientsRead)]
    public async Task<IActionResult> ListPatientContextMessages(Guid patientId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await contextService.ListPatientMessagesAsync(userId.Value, patientId, cancellationToken));
    }

    [HttpGet("context/guide/{guideId:guid}/messages")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> ListGuideContextMessages(
        Guid guideId,
        [FromQuery] string type = "tiss",
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await contextService.ListGuideMessagesAsync(userId.Value, guideId, type, cancellationToken));
    }

    [HttpGet("ai/quick-queries")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> GetAiQuickQueries(CancellationToken cancellationToken) =>
        Ok(await aiService.GetQuickQueriesAsync(cancellationToken));

    [HttpPost("ai/ask")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> AskAi(
        [FromBody] ConnectAiAskRequest request,
        [FromQuery] bool stream = false,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (!stream)
        {
            return Ok(await aiService.AskAsync(userId.Value, request, cancellationToken));
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        await foreach (var chunk in aiService.AskStreamAsync(userId.Value, request, cancellationToken))
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = chunk.Type,
                text = chunk.Text,
                intent = chunk.Intent,
                usedLlm = chunk.UsedLlm,
                data = chunk.Data,
            });

            await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        return new EmptyResult();
    }

    [HttpPost("ai/ask/stream")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public Task<IActionResult> AskAiStream(
        [FromBody] ConnectAiAskRequest request,
        CancellationToken cancellationToken)
        => AskAi(request, stream: true, cancellationToken);

    private Guid? GetUserId()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
