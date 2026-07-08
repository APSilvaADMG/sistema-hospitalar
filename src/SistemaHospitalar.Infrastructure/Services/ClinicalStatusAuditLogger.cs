using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SistemaHospitalar.Application.DTOs.Audit;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Infrastructure.Services;

/// <summary>
/// RN-GER-005 / RN-ATD-007 — Auditoria de mudanças críticas de status.
/// </summary>
public class ClinicalStatusAuditLogger(IAuditService auditService, IHttpContextAccessor httpContextAccessor)
{
    public Task LogStatusChangeAsync(
        string entityType,
        Guid entityId,
        string action,
        string beforeStatus,
        string afterStatus,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var (userId, email) = ResolveActor();
        var message = details ?? $"Status alterado: {beforeStatus} → {afterStatus}";

        return auditService.LogAsync(new CreateAuditLogRequest(
            userId,
            email,
            action,
            entityType,
            entityId,
            message,
            httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            null,
            "StatusChange",
            true), cancellationToken);
    }

    public Task LogPatientInactivationAsync(
        Guid patientId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var (userId, email) = ResolveActor();

        return auditService.LogAsync(new CreateAuditLogRequest(
            userId,
            email,
            "InativarPaciente",
            "Patient",
            patientId,
            $"Paciente inativado. Motivo: {reason}",
            httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            null,
            "Inactivation",
            true), cancellationToken);
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
