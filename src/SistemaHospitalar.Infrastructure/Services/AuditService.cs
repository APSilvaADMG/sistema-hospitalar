using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Audit;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class AuditService(AppDbContext dbContext) : IAuditService
{
    public async Task LogAsync(CreateAuditLogRequest request, CancellationToken cancellationToken = default)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = request.UserId,
            UserEmail = request.UserEmail,
            Action = request.Action,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            Details = request.Details,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            DeviceId = request.DeviceId,
            ActionCategory = request.ActionCategory,
            IsSensitive = request.IsSensitive,
            BeforeSnapshot = request.BeforeSnapshot,
            AfterSnapshot = request.AfterSnapshot,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetLogsAsync(
        int limit, string? entityType, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 200);
        var query = dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType.Trim());
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new AuditLogDto(
                a.Id, a.UserEmail, a.Action, a.EntityType, a.EntityId,
                a.Details, a.IpAddress, a.UserAgent, a.ActionCategory, a.IsSensitive, a.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
