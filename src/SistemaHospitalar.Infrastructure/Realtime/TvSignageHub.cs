using Microsoft.AspNetCore.SignalR;

namespace SistemaHospitalar.Infrastructure.Realtime;

public class TvSignageHub : Hub
{
    public static string DisplayGroup(Guid displayId) => $"tv-display-{displayId:N}";

    public async Task RegisterDisplay(string slug, string token)
    {
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(token))
        {
            throw new HubException("Credenciais da TV inválidas.");
        }

        var displayId = await TvSignageHubAuth.TryResolveDisplayIdAsync(Context, slug, token);
        if (displayId is null)
        {
            throw new HubException("TV não autorizada.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, DisplayGroup(displayId.Value));
    }
}
