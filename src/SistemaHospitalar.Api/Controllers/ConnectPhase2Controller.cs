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
public class ConnectPhase2Controller(
    IConnectTicketService ticketService,
    IConnectTaskService taskService,
    IConnectWorkflowService workflowService) : ControllerBase
{
    [HttpGet("tickets/summary")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> GetTicketSummary(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await ticketService.GetSummaryAsync(userId.Value, cancellationToken));
    }

    [HttpGet("tickets")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> ListTickets(
        [FromQuery] ConnectTicketStatus? status,
        [FromQuery] ConnectTicketCategory? category,
        [FromQuery] MessagePriority? priority,
        [FromQuery] bool? assignedToMe,
        [FromQuery] bool? myRequests,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await ticketService.ListAsync(
            userId.Value, status, category, priority, assignedToMe, myRequests, search, cancellationToken));
    }

    [HttpGet("tickets/{id:guid}")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var ticket = await ticketService.GetAsync(userId.Value, id, cancellationToken);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost("tickets")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> CreateTicket([FromBody] CreateConnectTicketRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await ticketService.CreateAsync(userId.Value, request, cancellationToken));
    }

    [HttpPut("tickets/{id:guid}")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> UpdateTicket(Guid id, [FromBody] UpdateConnectTicketRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var ticket = await ticketService.UpdateAsync(userId.Value, id, request, cancellationToken);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost("tickets/{id:guid}/assign")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> AssignTicket(Guid id, [FromBody] AssignConnectTicketRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var ticket = await ticketService.AssignAsync(userId.Value, id, request, cancellationToken);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost("tickets/{id:guid}/status")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> ChangeTicketStatus(Guid id, [FromBody] ChangeConnectTicketStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var ticket = await ticketService.ChangeStatusAsync(userId.Value, id, request, cancellationToken);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost("tickets/{id:guid}/comments")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> AddTicketComment(Guid id, [FromBody] AddConnectTicketCommentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var comment = await ticketService.AddCommentAsync(userId.Value, id, request, cancellationToken);
        return comment is null ? NotFound() : Ok(comment);
    }

    [HttpDelete("tickets/{id:guid}")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> DeleteTicket(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return await ticketService.DeleteAsync(userId.Value, id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpGet("tasks/summary")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> GetTaskSummary(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await taskService.GetSummaryAsync(userId.Value, cancellationToken));
    }

    [HttpGet("tasks")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> ListTasks(
        [FromQuery] string scope = "all",
        [FromQuery] ConnectTaskStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await taskService.ListAsync(userId.Value, scope, status, cancellationToken));
    }

    [HttpGet("tasks/{id:guid}")]
    [RequirePermission(PermissionCodes.ConnectRead)]
    public async Task<IActionResult> GetTask(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var task = await taskService.GetAsync(userId.Value, id, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost("tasks")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> CreateTask([FromBody] CreateConnectTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await taskService.CreateAsync(userId.Value, request, cancellationToken));
    }

    [HttpPut("tasks/{id:guid}")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateConnectTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var task = await taskService.UpdateAsync(userId.Value, id, request, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost("tasks/{id:guid}/status")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> ChangeTaskStatus(Guid id, [FromBody] ChangeConnectTaskStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var task = await taskService.ChangeStatusAsync(userId.Value, id, request, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpDelete("tasks/{id:guid}")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return await taskService.DeleteAsync(userId.Value, id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpGet("approvals/summary")]
    [RequireAnyPermission(PermissionCodes.ConnectApprove, PermissionCodes.ConnectRead)]
    public async Task<IActionResult> GetApprovalSummary(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await workflowService.GetSummaryAsync(userId.Value, cancellationToken));
    }

    [HttpGet("approvals")]
    [RequireAnyPermission(PermissionCodes.ConnectApprove, PermissionCodes.ConnectRead)]
    public async Task<IActionResult> ListApprovals([FromQuery] bool? pendingForMe, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await workflowService.ListAsync(userId.Value, pendingForMe, cancellationToken));
    }

    [HttpGet("approvals/{id:guid}")]
    [RequireAnyPermission(PermissionCodes.ConnectApprove, PermissionCodes.ConnectRead)]
    public async Task<IActionResult> GetApproval(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var instance = await workflowService.GetAsync(userId.Value, id, cancellationToken);
        return instance is null ? NotFound() : Ok(instance);
    }

    [HttpPost("approvals")]
    [RequirePermission(PermissionCodes.ConnectWrite)]
    public async Task<IActionResult> CreateApproval([FromBody] CreateWorkflowInstanceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        try
        {
            return Ok(await workflowService.CreateAsync(userId.Value, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("approvals/{id:guid}/approve")]
    [RequirePermission(PermissionCodes.ConnectApprove)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] WorkflowDecisionRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var instance = await workflowService.ApproveAsync(userId.Value, id, request, cancellationToken);
        return instance is null ? NotFound() : Ok(instance);
    }

    [HttpPost("approvals/{id:guid}/reject")]
    [RequirePermission(PermissionCodes.ConnectApprove)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] WorkflowDecisionRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var instance = await workflowService.RejectAsync(userId.Value, id, request, cancellationToken);
        return instance is null ? NotFound() : Ok(instance);
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
