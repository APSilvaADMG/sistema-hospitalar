using System.Text.Json;
using SistemaHospitalar.Mobile.Models;

namespace SistemaHospitalar.Mobile.Services;

public class SyncEngine(
    LocalDatabase db,
    ApiClient api,
    ConnectivityService connectivity,
    SecureDatabaseService secureDatabase)
{
    public string DeviceId { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var key = await secureDatabase.GetOrCreateKeyAsync();
        await db.InitializeAsync(key);
        var stored = await db.GetMetaAsync("deviceId");
        if (string.IsNullOrWhiteSpace(stored))
        {
            DeviceId = Guid.NewGuid().ToString();
            await db.SetMetaAsync("deviceId", DeviceId);
        }
        else
        {
            DeviceId = stored;
        }
    }

    public async Task<bool> SyncAsync(CancellationToken ct = default)
    {
        if (!connectivity.IsOnline)
        {
            return false;
        }

        await PushPendingAsync(ct);
        await PullAsync(ct);
        return true;
    }

    public async Task EnqueueTransportAdvanceAsync(Guid requestId, string status)
    {
        var mutation = new OutboxMutation
        {
            Entity = "TransportRequest",
            Action = "Advance",
            PayloadJson = JsonSerializer.Serialize(new { requestId, status }),
            ClientTimestamp = DateTime.UtcNow,
        };

        await db.InsertMutationAsync(mutation);
        await ApplyLocalTransportStatusAsync(requestId, status);
    }

    public async Task EnqueueTransportAcceptAsync(Guid requestId, Guid employeeId, Guid? assetId)
    {
        var mutation = new OutboxMutation
        {
            Entity = "TransportRequest",
            Action = "Accept",
            PayloadJson = JsonSerializer.Serialize(new { requestId, employeeId, transportAssetId = assetId }),
            ClientTimestamp = DateTime.UtcNow,
        };

        await db.InsertMutationAsync(mutation);

        var cached = await db.GetTransportAsync(requestId);
        if (cached is not null)
        {
            cached.Status = "Accepted";
            cached.AcceptedAt = DateTime.UtcNow;
            await db.UpsertTransportAsync(cached);
        }
    }

    private async Task PushPendingAsync(CancellationToken ct)
    {
        var pending = await db.GetPendingMutationsAsync();
        if (pending.Count == 0)
        {
            return;
        }

        var items = pending.Select(m => new SyncMutationItem(
            m.ClientMutationId,
            m.Entity,
            m.Action,
            JsonSerializer.Deserialize<object>(m.PayloadJson) ?? new { },
            m.ClientTimestamp)).ToList();

        var result = await api.PushAsync(DeviceId, items, ct);
        if (result is null)
        {
            return;
        }

        foreach (var item in result.Results)
        {
            var local = pending.First(p => p.ClientMutationId == item.ClientMutationId);
            local.Status = item.Status is "Applied" or "Duplicate" ? "Synced" : "Failed";
            local.ErrorMessage = item.Message;
            await db.UpdateMutationAsync(local);
        }
    }

    private async Task PullAsync(CancellationToken ct)
    {
        var sinceRaw = await db.GetMetaAsync("lastPull");
        DateTime? since = DateTime.TryParse(sinceRaw, out var parsed) ? parsed : null;

        var pull = await api.PullAsync(since, ct);
        if (pull is null)
        {
            return;
        }

        var cached = pull.TransportRequests.Select(t => new CachedTransportRequest
        {
            Id = t.Id,
            PatientName = t.PatientName,
            OriginType = t.OriginType,
            OriginDetail = t.OriginDetail,
            DestinationType = t.DestinationType,
            DestinationDetail = t.DestinationDetail,
            Status = t.Status,
            Priority = t.Priority,
            AssignedEmployeeName = t.AssignedEmployeeName,
            TransportAssetCode = t.TransportAssetCode,
            RequestedAt = t.RequestedAt,
            AcceptedAt = t.AcceptedAt,
            CompletedAt = t.CompletedAt,
            Json = JsonSerializer.Serialize(t),
        }).ToList();

        await db.ReplaceTransportsAsync(cached);
        await db.SetMetaAsync("lastPull", pull.ServerTimestamp.ToUniversalTime().ToString("O"));

        if (pull.Porters.Count > 0)
        {
            await db.SetMetaAsync("defaultPorterId", pull.Porters[0].Id.ToString());
        }
    }

    private async Task ApplyLocalTransportStatusAsync(Guid requestId, string status)
    {
        var cached = await db.GetTransportAsync(requestId);
        if (cached is null)
        {
            return;
        }

        cached.Status = status;
        if (status == "InTransit")
        {
            cached.AcceptedAt ??= DateTime.UtcNow;
        }
        if (status == "Completed")
        {
            cached.CompletedAt = DateTime.UtcNow;
        }

        await db.UpsertTransportAsync(cached);
    }
}

public class ConnectivityService
{
    public bool IsOnline => Connectivity.NetworkAccess == NetworkAccess.Internet;
}
