using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/tasks")]
public class TasksController(ITaskEngineService taskEngineService) : ControllerBase
{
    [HttpGet("my-missions")]
    public async Task<IActionResult> GetMyMissions(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await taskEngineService.GenerateTasksForUserAsync(userId.Value, cancellationToken));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var ok = await taskEngineService.CompleteTaskAsync(id, userId.Value, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
