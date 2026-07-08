using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Infrastructure.Realtime;

public class OperationsRealtimeNotifier(
    IHubContext<OperationsHub> hubContext,
    ILogger<OperationsRealtimeNotifier> logger) : IOperationsRealtimeNotifier
{
    public async Task NotifyTransportChangedAsync(Guid? requestId = null, CancellationToken cancellationToken = default)
    {
        await BroadcastAsync("transportChanged", new { requestId, at = DateTime.UtcNow }, cancellationToken);
        await BroadcastAsync("syncRequired", new { scope = "transport", at = DateTime.UtcNow }, cancellationToken);
    }

    public async Task NotifyCleaningChangedAsync(Guid? requestId = null, CancellationToken cancellationToken = default)
    {
        await BroadcastAsync("cleaningChanged", new { requestId, at = DateTime.UtcNow }, cancellationToken);
        await BroadcastAsync("syncRequired", new { scope = "cleaning", at = DateTime.UtcNow }, cancellationToken);
    }

    public async Task NotifyBedsChangedAsync(CancellationToken cancellationToken = default)
    {
        await BroadcastAsync("bedsChanged", new { at = DateTime.UtcNow }, cancellationToken);
        await BroadcastAsync("syncRequired", new { scope = "beds", at = DateTime.UtcNow }, cancellationToken);
    }

    private async Task BroadcastAsync(string eventName, object payload, CancellationToken cancellationToken)
    {
        try
        {
            await hubContext.Clients
                .Group(OperationsHub.OperationsGroup)
                .SendAsync(eventName, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar evento SignalR {EventName}", eventName);
        }
    }
}
