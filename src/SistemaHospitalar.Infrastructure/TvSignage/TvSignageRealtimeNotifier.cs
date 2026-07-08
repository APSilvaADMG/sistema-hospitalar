using Microsoft.AspNetCore.SignalR;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Infrastructure.Realtime;

namespace SistemaHospitalar.Infrastructure.TvSignage;

public class TvSignageRealtimeNotifier(IHubContext<TvSignageHub> hubContext) : ITvSignageRealtimeNotifier
{
    public Task NotifyDisplayUpdatedAsync(Guid displayId, CancellationToken cancellationToken = default)
        => hubContext.Clients.Group(TvSignageHub.DisplayGroup(displayId)).SendAsync("tvStateChanged", cancellationToken);

    public Task NotifyQueueCallAsync(Guid? displayId, CancellationToken cancellationToken = default)
    {
        if (displayId is null)
        {
            return hubContext.Clients.All.SendAsync("tvQueueCall", cancellationToken);
        }

        return hubContext.Clients
            .Group(TvSignageHub.DisplayGroup(displayId.Value))
            .SendAsync("tvQueueCall", cancellationToken);
    }
}
