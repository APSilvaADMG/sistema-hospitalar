using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.TvSignage;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;

namespace SistemaHospitalar.Api.Controllers;

[ApiController]
[Route("api/tv-signage")]
public class TvSignageController(ITvSignageService tvSignageService) : ControllerBase
{
    [HttpGet("monitor")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectRead}")]
    public async Task<IActionResult> GetMonitor(CancellationToken cancellationToken)
        => Ok(await tvSignageService.GetMonitorSummaryAsync(cancellationToken));

    [HttpGet("displays")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectRead}")]
    public async Task<IActionResult> ListDisplays(CancellationToken cancellationToken)
        => Ok(await tvSignageService.ListDisplaysAsync(cancellationToken));

    [HttpPost("displays")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    public async Task<IActionResult> CreateDisplay([FromBody] CreateTvDisplayRequest request, CancellationToken cancellationToken)
        => Ok(await tvSignageService.CreateDisplayAsync(request, cancellationToken));

    [HttpPut("displays/{id:guid}")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    public async Task<IActionResult> UpdateDisplay(Guid id, [FromBody] UpdateTvDisplayRequest request, CancellationToken cancellationToken)
    {
        var result = await tvSignageService.UpdateDisplayAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("displays/{id:guid}")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    public async Task<IActionResult> DeleteDisplay(Guid id, CancellationToken cancellationToken)
        => await tvSignageService.DeleteDisplayAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("displays/{id:guid}/regenerate-token")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    public async Task<IActionResult> RegenerateToken(Guid id, CancellationToken cancellationToken)
    {
        var token = await tvSignageService.RegenerateDisplayTokenAsync(id, cancellationToken);
        return token is null ? NotFound() : Ok(new { token });
    }

    [HttpGet("layouts")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectRead}")]
    public async Task<IActionResult> ListLayouts(CancellationToken cancellationToken)
        => Ok(await tvSignageService.ListLayoutsAsync(cancellationToken));

    [HttpPost("layouts")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    public async Task<IActionResult> CreateLayout([FromBody] CreateTvLayoutRequest request, CancellationToken cancellationToken)
        => Ok(await tvSignageService.CreateLayoutAsync(request, cancellationToken));

    [HttpPut("layouts/{id:guid}")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    public async Task<IActionResult> UpdateLayout(Guid id, [FromBody] UpdateTvLayoutRequest request, CancellationToken cancellationToken)
    {
        var result = await tvSignageService.UpdateLayoutAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("media")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectRead}")]
    public async Task<IActionResult> ListMedia(CancellationToken cancellationToken)
        => Ok(await tvSignageService.ListMediaAsync(cancellationToken));

    [HttpPost("media")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    [RequestSizeLimit(100_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadMedia(
        [FromForm] UploadTvMediaForm form,
        CancellationToken cancellationToken = default)
    {
        if (form.File.Length == 0) return BadRequest("Arquivo vazio.");
        await using var stream = new MemoryStream();
        await form.File.CopyToAsync(stream, cancellationToken);
        var request = new CreateTvMediaRequest(
            form.Title,
            (Domain.Enums.TvMediaType)form.MediaType,
            form.Sector,
            form.StartsAt,
            form.EndsAt,
            form.Priority,
            form.DurationSeconds);
        return Ok(await tvSignageService.UploadMediaAsync(request, form.File.FileName, stream.ToArray(), cancellationToken));
    }

    [HttpDelete("media/{id:guid}")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    public async Task<IActionResult> DeleteMedia(Guid id, CancellationToken cancellationToken)
        => await tvSignageService.DeleteMediaAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("news")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectRead}")]
    public async Task<IActionResult> ListNews(CancellationToken cancellationToken)
        => Ok(await tvSignageService.ListNewsAsync(cancellationToken));

    [HttpPost("news")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    public async Task<IActionResult> CreateNews([FromBody] CreateTvNewsRequest request, CancellationToken cancellationToken)
        => Ok(await tvSignageService.CreateNewsAsync(request, cancellationToken));

    [HttpDelete("news/{id:guid}")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    public async Task<IActionResult> DeleteNews(Guid id, CancellationToken cancellationToken)
        => await tvSignageService.DeleteNewsAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("announcements")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectRead}")]
    public async Task<IActionResult> ListAnnouncements(CancellationToken cancellationToken)
        => Ok(await tvSignageService.ListAnnouncementsAsync(cancellationToken));

    [HttpPost("announcements")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    public async Task<IActionResult> CreateAnnouncement([FromBody] CreateTvAnnouncementRequest request, CancellationToken cancellationToken)
        => Ok(await tvSignageService.CreateAnnouncementAsync(request, cancellationToken));

    [HttpDelete("announcements/{id:guid}")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    public async Task<IActionResult> DeleteAnnouncement(Guid id, CancellationToken cancellationToken)
        => await tvSignageService.DeleteAnnouncementAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("calls")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectRead}")]
    public async Task<IActionResult> ListCalls([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
        => Ok(await tvSignageService.ListRecentCallsAsync(limit, cancellationToken));

    [HttpPost("calls")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectWrite}")]
    public async Task<IActionResult> CallQueue([FromBody] CallTvQueueRequest request, CancellationToken cancellationToken)
        => Ok(await tvSignageService.CallQueueAsync(request, cancellationToken));

    [HttpPost("kiosk/{kioskTicketId:guid}/call")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectWrite}")]
    public async Task<IActionResult> CallKioskTicket(Guid kioskTicketId, [FromBody] CallKioskTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await tvSignageService.CallKioskTicketAsync(kioskTicketId, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("campaigns")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectRead}")]
    public async Task<IActionResult> ListCampaigns(CancellationToken cancellationToken)
        => Ok(await tvSignageService.ListCampaignsAsync(cancellationToken));

    [HttpPost("campaigns")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateTvCampaignRequest request, CancellationToken cancellationToken)
        => Ok(await tvSignageService.CreateCampaignAsync(request, cancellationToken));

    [HttpPut("campaigns/{id:guid}")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    public async Task<IActionResult> UpdateCampaign(Guid id, [FromBody] UpdateTvCampaignRequest request, CancellationToken cancellationToken)
    {
        var result = await tvSignageService.UpdateCampaignAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("campaigns/{id:guid}")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectAdmin}")]
    public async Task<IActionResult> DeleteCampaign(Guid id, CancellationToken cancellationToken)
        => await tvSignageService.DeleteCampaignAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("speech/provider")]
    [Authorize(Policy = $"perm:{PermissionCodes.ConnectRead}")]
    public IActionResult GetSpeechProvider()
        => Ok(new { provider = tvSignageService.GetSpeechProvider() });

    [HttpGet("player/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlayerState(string slug, [FromQuery] string token, CancellationToken cancellationToken)
    {
        var state = await tvSignageService.GetPlayerStateAsync(slug, token, cancellationToken);
        return state is null ? NotFound() : Ok(state);
    }

    [HttpPost("player/{slug}/heartbeat")]
    [AllowAnonymous]
    public async Task<IActionResult> Heartbeat(string slug, [FromQuery] string token, [FromBody] TvHeartbeatRequest request, CancellationToken cancellationToken)
        => await tvSignageService.RegisterHeartbeatAsync(slug, token, request, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("player/{slug}/speech/{callId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCallSpeech(string slug, Guid callId, [FromQuery] string token, CancellationToken cancellationToken)
    {
        var audio = await tvSignageService.GetCallSpeechAsync(slug, token, callId, cancellationToken);
        return audio is null ? NotFound() : File(audio, "audio/mpeg");
    }
}

public sealed class UploadTvMediaForm
{
    public string Title { get; set; } = string.Empty;
    public int MediaType { get; set; }
    public IFormFile File { get; set; } = null!;
    public string? Sector { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int Priority { get; set; }
    public int DurationSeconds { get; set; } = 15;
}
