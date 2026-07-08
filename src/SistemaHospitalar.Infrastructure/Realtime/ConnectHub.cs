using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SistemaHospitalar.Infrastructure.Realtime;

[Authorize(Policy = "connect.realtime")]
public class ConnectHub : Hub
{
    public const string InboxGroup = "connect-inbox";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, InboxGroup);

        var userId = Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"connect-user-{userId}");
        }

        await base.OnConnectedAsync();
    }
}