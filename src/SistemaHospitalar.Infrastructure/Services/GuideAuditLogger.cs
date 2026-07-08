using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SistemaHospitalar.Application.DTOs.Audit;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Services;

public class GuideAuditLogger(IAuditService auditService, IHttpContextAccessor httpContextAccessor)
{
    public Task LogTissGuideAsync(
        Guid guideId,
        string action,
        string details,
        TissGuideStatus? before = null,
        TissGuideStatus? after = null,
        CancellationToken cancellationToken = default)
    {
        var (userId, email) = ResolveActor();
        if (before.HasValue && after.HasValue)
        {
            details = $"{details} (status: {before} → {after})";
        }

        return auditService.LogAsync(new CreateAuditLogRequest(
            userId,
            email,
            action,
            "TissGuide",
            guideId,
            details,
            httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            null,
            "billing",
            false), cancellationToken);
    }

    public Task LogSusGuideAsync(
        Guid guideId,
        string action,
        string details,
        SusGuideStatus? before = null,
        SusGuideStatus? after = null,
        CancellationToken cancellationToken = default)
    {
        var (userId, email) = ResolveActor();
        if (before.HasValue && after.HasValue)
        {
            details = $"{details} (status: {before} → {after})";
        }

        return auditService.LogAsync(new CreateAuditLogRequest(
            userId,
            email,
            action,
            "SusGuide",
            guideId,
            details,
            httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            null,
            "billing",
            false), cancellationToken);
    }

    private (Guid? UserId, string Email) ResolveActor()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return (null, "system");
        }

        var email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? "system";
        var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");
        Guid? userId = Guid.TryParse(idClaim, out var parsed) ? parsed : null;
        return (userId, email);
    }
}
