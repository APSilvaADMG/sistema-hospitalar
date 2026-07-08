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
public class ConnectCommunicationController(
    IConnectMailService mailService,
    IConnectChatService chatService,
    IConnectNotificationService notificationService,
    IBulletinService bulletinService,
    IConnectCommSummaryService summaryService) : ControllerBase
{
    [HttpGet("comm/summary")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await summaryService.GetSummaryAsync(userId.Value, cancellationToken));
    }

    [HttpGet("mail")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> ListMail(
        [FromQuery] MailFolder folder = MailFolder.Inbox,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await mailService.ListAsync(userId.Value, folder, search, cancellationToken));
    }

    [HttpGet("mail/{id:guid}")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> GetMail(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var detail = await mailService.GetAsync(userId.Value, id, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("mail")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> CreateMail([FromBody] CreateMailRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await mailService.CreateAsync(userId.Value, request, cancellationToken));
    }

    [HttpPut("mail/{id:guid}")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> UpdateDraft(Guid id, [FromBody] UpdateMailRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var detail = await mailService.UpdateDraftAsync(userId.Value, id, request, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("mail/{id:guid}/send")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> SendDraft(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var detail = await mailService.SendDraftAsync(userId.Value, id, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("mail/{id:guid}/read")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> MarkMailRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return await mailService.MarkReadAsync(userId.Value, id, cancellationToken) ? Ok() : NotFound();
    }

    [HttpPost("mail/{id:guid}/archive")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> ArchiveMail(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return await mailService.ArchiveAsync(userId.Value, id, cancellationToken) ? Ok() : NotFound();
    }

    [HttpPost("mail/{id:guid}/trash")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> TrashMail(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return await mailService.TrashAsync(userId.Value, id, cancellationToken) ? Ok() : NotFound();
    }

    [HttpGet("mail/{messageId:guid}/attachments/{attachmentId:guid}")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> DownloadMailAttachment(
        Guid messageId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var file = await mailService.GetAttachmentAsync(userId.Value, messageId, attachmentId, cancellationToken);
        if (file is null) return NotFound();

        return File(file.Value.Content, file.Value.MimeType, file.Value.FileName);
    }

    [HttpGet("chat/rooms")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> ListChatRooms(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await chatService.ListRoomsAsync(userId.Value, cancellationToken));
    }

    [HttpPost("chat/rooms")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> CreateChatRoom([FromBody] CreateChatRoomRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var room = await chatService.CreateRoomAsync(userId.Value, request, cancellationToken);
        return room is null ? BadRequest() : Ok(room);
    }

    [HttpGet("chat/rooms/{id:guid}/messages")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> ListChatMessages(Guid id, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await chatService.ListMessagesAsync(userId.Value, id, limit, cancellationToken));
    }

    [HttpPost("chat/rooms/{id:guid}/messages")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> SendChatMessage(Guid id, [FromBody] SendChatMessageRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var message = await chatService.SendMessageAsync(userId.Value, id, request, cancellationToken);
        return message is null ? NotFound() : Ok(message);
    }

    [HttpPost("chat/rooms/{id:guid}/read")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> MarkChatRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return await chatService.MarkRoomReadAsync(userId.Value, id, cancellationToken) ? Ok() : NotFound();
    }

    [HttpGet("connect-notifications")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> ListNotifications([FromQuery] bool? unreadOnly, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await notificationService.ListAsync(userId.Value, unreadOnly, cancellationToken));
    }

    [HttpGet("connect-notifications/unread-count")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> GetNotificationUnreadCount(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var count = await notificationService.GetUnreadCountAsync(userId.Value, cancellationToken);
        return Ok(new { count });
    }

    [HttpPost("connect-notifications/{id:guid}/read")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> MarkNotificationRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return await notificationService.MarkReadAsync(userId.Value, id, cancellationToken) ? Ok() : NotFound();
    }

    [HttpGet("bulletin")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> ListBulletin(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await bulletinService.ListAsync(userId.Value, cancellationToken));
    }

    [HttpGet("bulletin/{id:guid}")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> GetBulletin(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var post = await bulletinService.GetAsync(userId.Value, id, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpPost("bulletin")]
    [RequirePermission(PermissionCodes.ConnectAdmin)]
    public async Task<IActionResult> CreateBulletin([FromBody] CreateBulletinPostRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await bulletinService.CreateAsync(userId.Value, request, cancellationToken));
    }

    [HttpPut("bulletin/{id:guid}")]
    [RequirePermission(PermissionCodes.ConnectAdmin)]
    public async Task<IActionResult> UpdateBulletin(Guid id, [FromBody] UpdateBulletinPostRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var post = await bulletinService.UpdateAsync(userId.Value, id, request, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpDelete("bulletin/{id:guid}")]
    [RequirePermission(PermissionCodes.ConnectAdmin)]
    public async Task<IActionResult> DeleteBulletin(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return await bulletinService.DeleteAsync(userId.Value, id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("bulletin/{id:guid}/view")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> MarkBulletinViewed(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return await bulletinService.MarkViewedAsync(userId.Value, id, cancellationToken) ? Ok() : NotFound();
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
