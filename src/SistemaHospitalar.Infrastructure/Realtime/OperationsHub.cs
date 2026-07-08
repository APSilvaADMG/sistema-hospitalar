using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SistemaHospitalar.Infrastructure.Realtime;

[Authorize(Policy = "operations.realtime")]
public class OperationsHub : Hub
{
    public const string OperationsGroup = "operations";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, OperationsGroup);
        await base.OnConnectedAsync();
    }
}
