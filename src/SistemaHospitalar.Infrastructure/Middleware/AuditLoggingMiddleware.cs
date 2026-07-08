using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using SistemaHospitalar.Application.DTOs.Audit;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Infrastructure.Middleware;

public partial class AuditLoggingMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> AuditedWriteMethods =
        ["POST", "PUT", "PATCH", "DELETE"];

    private static readonly string[] SensitiveReadPrefixes =
    [
        "/api/patients/",
        "/api/medical-record",
        "/api/digital-record",
        "/api/security",
        "/api/physical-security",
        "/api/lgpd",
        "/api/financial",
        "/api/pix",
        "/api/connect",
    ];

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        await next(context);

        if (context.User.Identity?.IsAuthenticated != true || context.Response.StatusCode >= 400)
        {
            return;
        }

        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        var isWrite = AuditedWriteMethods.Contains(method);
        var isSensitiveRead = method == "GET" && IsSensitiveRead(path);

        if (!isWrite && !isSensitiveRead)
        {
            return;
        }

        var userIdClaim = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        Guid? userId = Guid.TryParse(userIdClaim, out var id) ? id : null;
        var email = context.User.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? context.User.FindFirstValue(ClaimTypes.Email)
            ?? "unknown";

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var entityType = segments.Length >= 2 ? segments[1] : "api";
        Guid? entityId = TryParseEntityId(segments);

        var userAgent = context.Request.Headers.UserAgent.ToString();
        var deviceId = context.Request.Headers["X-Device-Id"].ToString();
        var category = isSensitiveRead ? "Read" : "Write";

        await auditService.LogAsync(new CreateAuditLogRequest(
            userId,
            email,
            method,
            entityType,
            entityId,
            $"{method} {path} → {context.Response.StatusCode}",
            context.Connection.RemoteIpAddress?.ToString(),
            string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
            string.IsNullOrWhiteSpace(deviceId) ? null : deviceId,
            category,
            isSensitiveRead || path.Contains("medical-record", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsSensitiveRead(string path)
        => SensitiveReadPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private static Guid? TryParseEntityId(string[] segments)
    {
        foreach (var segment in segments)
        {
            if (GuidRegex().IsMatch(segment) && Guid.TryParse(segment, out var guid))
            {
                return guid;
            }
        }

        return null;
    }

    [GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
    private static partial Regex GuidRegex();
}
