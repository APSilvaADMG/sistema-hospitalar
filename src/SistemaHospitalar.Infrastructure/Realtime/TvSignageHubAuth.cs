using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Realtime;

internal static class TvSignageHubAuth
{
    public static async Task<Guid?> TryResolveDisplayIdAsync(HubCallerContext context, string slug, string token)
    {
        var http = context.GetHttpContext();
        if (http is null) return null;

        var db = http.RequestServices.GetRequiredService<AppDbContext>();
        var display = await db.TvDisplays
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Slug == slug && d.PlayerToken == token && d.IsActive);

        return display?.Id;
    }
}
