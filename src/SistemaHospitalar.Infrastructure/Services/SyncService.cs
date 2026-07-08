using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.Serialization;
using SistemaHospitalar.Application.DTOs.Hotelaria;
using SistemaHospitalar.Application.DTOs.Sync;
using SistemaHospitalar.Application.DTOs.Transport;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class SyncService(
    AppDbContext dbContext,
    ITransportService transportService,
    IHotelariaHospitalarService hotelariaService,
    IOperationsRealtimeNotifier realtime) : ISyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new PortugueseEnumJsonConverterFactory(),
            new JsonStringEnumConverter(),
        },
    };

    private static readonly TransportRequestStatus[] ActiveTransportStatuses =
    [
        TransportRequestStatus.Queued,
        TransportRequestStatus.Accepted,
        TransportRequestStatus.InTransit
    ];

    public async Task<SyncPushResponse> PushAsync(
        SyncPushRequest request, Guid? userId, CancellationToken cancellationToken = default)
    {
        var results = new List<SyncMutationResultDto>();
        var deviceId = request.DeviceId.Trim();

        foreach (var mutation in request.Mutations)
        {
            var existing = await dbContext.SyncMutations
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ClientMutationId == mutation.ClientMutationId, cancellationToken);

            if (existing is not null)
            {
                results.Add(new SyncMutationResultDto(
                    mutation.ClientMutationId, "Duplicate", "Mutação já processada.", null));
                continue;
            }

            try
            {
                var (status, message, payload) = await ApplyMutationAsync(mutation, cancellationToken);

                dbContext.SyncMutations.Add(new SyncMutation
                {
                    ClientMutationId = mutation.ClientMutationId,
                    DeviceId = deviceId,
                    Entity = mutation.Entity,
                    Action = mutation.Action,
                    Status = status,
                    Message = message,
                    UserId = userId,
                    ClientTimestamp = mutation.ClientTimestamp,
                });

                await dbContext.SaveChangesAsync(cancellationToken);

                results.Add(new SyncMutationResultDto(
                    mutation.ClientMutationId, status, message, payload));
            }
            catch (Exception ex)
            {
                results.Add(new SyncMutationResultDto(
                    mutation.ClientMutationId, "Rejected", ex.Message, null));
            }
        }

        if (results.Any(r => r.Status is "Applied" or "Duplicate"))
        {
            await realtime.NotifyTransportChangedAsync(cancellationToken: cancellationToken);
            await realtime.NotifyCleaningChangedAsync(cancellationToken: cancellationToken);
        }

        return new SyncPushResponse(DateTime.UtcNow, results);
    }

    public async Task<SyncPullResponse> PullAsync(
        SyncPullRequest request, CancellationToken cancellationToken = default)
    {
        await TransportSeed.EnsureAsync(dbContext, cancellationToken);
        await HotelariaSeed.EnsureAsync(dbContext, cancellationToken);

        var since = request.Since ?? DateTime.UtcNow.AddHours(-24);

        var transportQuery = dbContext.TransportRequests
            .AsNoTracking()
            .Where(r => r.IsActive);

        transportQuery = transportQuery.Where(r =>
            ActiveTransportStatuses.Contains(r.Status)
            || r.UpdatedAt >= since
            || r.CreatedAt >= since);

        var transports = await transportQuery
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.RequestedAt)
            .Take(200)
            .Select(r => new TransportRequestDto(
                r.Id,
                r.PatientId,
                r.HospitalizationId,
                r.PatientName,
                r.OriginType,
                r.OriginDetail,
                r.DestinationType,
                r.DestinationDetail,
                r.Status,
                r.Priority,
                r.AssignedEmployeeId,
                r.AssignedEmployee != null ? r.AssignedEmployee.FullName : null,
                r.TransportAssetId,
                r.TransportAsset != null ? r.TransportAsset.Code : null,
                r.RequestedAt,
                r.AcceptedAt,
                r.ArrivedAtOriginAt,
                r.DepartedAt,
                r.ArrivedAtDestinationAt,
                r.CompletedAt,
                r.Notes,
                r.RequestedBy,
                r.SlaDeadlineAt,
                r.IsSlaViolated))
            .ToListAsync(cancellationToken);

        var cleaningQuery = dbContext.CleaningRequests
            .AsNoTracking()
            .Where(c => c.IsActive);

        cleaningQuery = cleaningQuery.Where(c =>
            c.Status != CleaningRequestStatus.Completed && c.Status != CleaningRequestStatus.Cancelled
            || c.UpdatedAt >= since
            || c.CreatedAt >= since);

        var cleaningEntities = await cleaningQuery
            .Include(c => c.Bed).ThenInclude(b => b.Ward)
            .Include(c => c.AssignedEmployee)
            .OrderByDescending(c => c.RequestedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        var cleanings = cleaningEntities.Select(MapCleaning).ToList();

        var assets = await transportService.GetAssetsAsync(cancellationToken);
        var porters = await transportService.GetPortersAsync(cancellationToken);

        var bedsQuery = dbContext.Beds
            .AsNoTracking()
            .Where(b => b.IsActive)
            .Include(b => b.Ward)
            .AsQueryable();

        if (request.WardId.HasValue)
        {
            bedsQuery = bedsQuery.Where(b => b.WardId == request.WardId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(request.Sector))
        {
            var sector = request.Sector.Trim();
            bedsQuery = bedsQuery.Where(b => b.Ward.Name.Contains(sector) || b.Ward.Floor!.Contains(sector));
        }

        var beds = await bedsQuery
            .OrderBy(b => b.Ward.Name)
            .ThenBy(b => b.BedNumber)
            .Take(300)
            .Select(b => new BedSyncDto(
                b.Id,
                b.WardId,
                b.Ward.Name,
                b.BedNumber,
                b.Status.ToString(),
                b.StatusReason,
                b.UpdatedAt ?? b.CreatedAt))
            .ToListAsync(cancellationToken);

        return new SyncPullResponse(
            DateTime.UtcNow,
            transports,
            cleanings,
            assets,
            porters,
            beds);
    }

    private async Task<(string Status, string? Message, JsonElement? Payload)> ApplyMutationAsync(
        SyncMutationItemDto mutation, CancellationToken cancellationToken)
    {
        return mutation.Entity switch
        {
            "TransportRequest" => await ApplyTransportMutationAsync(mutation, cancellationToken),
            "CleaningRequest" => await ApplyCleaningMutationAsync(mutation, cancellationToken),
            _ => ("Rejected", $"Entidade não suportada: {mutation.Entity}", null),
        };
    }

    private async Task<(string Status, string? Message, JsonElement? Payload)> ApplyTransportMutationAsync(
        SyncMutationItemDto mutation, CancellationToken cancellationToken)
    {
        var requestId = GetGuid(mutation.Payload, "requestId");
        if (requestId is null)
        {
            return ("Rejected", "requestId obrigatório.", null);
        }

        if (await HasConflictAsync<TransportRequest>(requestId.Value, mutation.ClientTimestamp, cancellationToken))
        {
            return ("Conflict", "Registro alterado no servidor após o timestamp do dispositivo.", null);
        }

        switch (mutation.Action)
        {
            case "Accept":
            {
                var employeeId = GetGuid(mutation.Payload, "employeeId");
                if (employeeId is null)
                {
                    return ("Rejected", "employeeId obrigatório.", null);
                }

                var assetId = GetGuid(mutation.Payload, "transportAssetId");
                var result = await transportService.AcceptRequestAsync(
                    requestId.Value,
                    new AcceptTransportRequestRequest(employeeId.Value, assetId),
                    cancellationToken);

                return result is null
                    ? ("Rejected", "Solicitação não encontrada ou status inválido.", null)
                    : ("Applied", null, ToJson(result));
            }
            case "Advance":
            {
                var status = GetEnum<TransportRequestStatus>(mutation.Payload, "status");
                if (status is null)
                {
                    return ("Rejected", "status obrigatório.", null);
                }

                var result = await transportService.AdvanceRequestAsync(
                    requestId.Value,
                    new AdvanceTransportRequestRequest(status.Value),
                    cancellationToken);

                return result is null
                    ? ("Rejected", "Solicitação não encontrada.", null)
                    : ("Applied", null, ToJson(result));
            }
            case "Cancel":
            {
                var result = await transportService.CancelRequestAsync(requestId.Value, cancellationToken);
                return result is null
                    ? ("Rejected", "Solicitação não encontrada.", null)
                    : ("Applied", null, ToJson(result));
            }
            default:
                return ("Rejected", $"Ação não suportada: {mutation.Action}", null);
        }
    }

    private async Task<(string Status, string? Message, JsonElement? Payload)> ApplyCleaningMutationAsync(
        SyncMutationItemDto mutation, CancellationToken cancellationToken)
    {
        var requestId = GetGuid(mutation.Payload, "requestId");
        if (requestId is null)
        {
            return ("Rejected", "requestId obrigatório.", null);
        }

        if (await HasConflictAsync<CleaningRequest>(requestId.Value, mutation.ClientTimestamp, cancellationToken))
        {
            return ("Conflict", "Registro alterado no servidor após o timestamp do dispositivo.", null);
        }

        switch (mutation.Action)
        {
            case "Start":
            {
                var team = GetString(mutation.Payload, "assignedTeam");
                var employeeId = GetGuid(mutation.Payload, "assignedEmployeeId");
                var result = await hotelariaService.StartCleaningAsync(
                    requestId.Value,
                    new StartCleaningRequestRequest(team, employeeId),
                    cancellationToken);

                return result is null
                    ? ("Rejected", "Higienização não encontrada ou status inválido.", null)
                    : ("Applied", null, ToJson(result));
            }
            case "Complete":
            {
                var notes = GetString(mutation.Payload, "notes");
                var result = await hotelariaService.CompleteCleaningAsync(
                    requestId.Value,
                    new CompleteCleaningRequestRequest(notes),
                    cancellationToken);

                return result is null
                    ? ("Rejected", "Higienização não encontrada.", null)
                    : ("Applied", null, ToJson(result));
            }
            case "UpdateChecklist":
            {
                if (!mutation.Payload.TryGetProperty("checklist", out var checklistElement))
                {
                    return ("Rejected", "checklist obrigatório.", null);
                }

                var checklist = JsonSerializer.Deserialize<List<CleaningChecklistItemDto>>(
                    checklistElement.GetRawText(), JsonOptions) ?? [];

                var result = await hotelariaService.UpdateChecklistAsync(
                    requestId.Value,
                    new UpdateCleaningChecklistRequest(checklist),
                    cancellationToken);

                return result is null
                    ? ("Rejected", "Higienização não encontrada.", null)
                    : ("Applied", null, ToJson(result));
            }
            default:
                return ("Rejected", $"Ação não suportada: {mutation.Action}", null);
        }
    }

    private async Task<bool> HasConflictAsync<TEntity>(
        Guid id, DateTime clientTimestamp, CancellationToken cancellationToken)
        where TEntity : BaseEntity
    {
        var entity = await dbContext.Set<TEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        var serverTime = entity.UpdatedAt ?? entity.CreatedAt;
        return serverTime > clientTimestamp.ToUniversalTime().AddSeconds(2);
    }

    private static Guid? GetGuid(JsonElement payload, string name)
    {
        if (!payload.TryGetProperty(name, out var prop))
        {
            return null;
        }

        if (prop.ValueKind == JsonValueKind.String && Guid.TryParse(prop.GetString(), out var guid))
        {
            return guid;
        }

        return null;
    }

    private static string? GetString(JsonElement payload, string name)
    {
        if (!payload.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return prop.GetString();
    }

    private static TEnum? GetEnum<TEnum>(JsonElement payload, string name)
        where TEnum : struct, Enum
    {
        if (!payload.TryGetProperty(name, out var prop))
        {
            return null;
        }

        if (prop.ValueKind == JsonValueKind.String && Enum.TryParse<TEnum>(prop.GetString(), true, out var parsed))
        {
            return parsed;
        }

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var num))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), num);
        }

        return null;
    }

    private static JsonElement? ToJson<T>(T value)
    {
        var json = JsonSerializer.SerializeToElement(value, JsonOptions);
        return json;
    }

    private static CleaningRequestDto MapCleaning(CleaningRequest c) => new(
        c.Id,
        c.BedId,
        c.Bed?.Ward?.Name ?? "—",
        c.Bed?.BedNumber ?? "—",
        c.HospitalizationId,
        c.CleaningType,
        c.Status,
        c.TriggerReason,
        c.AssignedTeam,
        c.AssignedEmployeeId,
        c.AssignedEmployee?.FullName,
        c.RequestedAt,
        c.StartedAt,
        c.CompletedAt,
        ParseChecklist(c.ChecklistJson),
        c.Notes);

    private static IReadOnlyList<CleaningChecklistItemDto> ParseChecklist(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<CleaningChecklistItemDto>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
