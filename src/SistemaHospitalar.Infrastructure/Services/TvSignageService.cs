using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.DTOs.TvSignage;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.TvSignage;

namespace SistemaHospitalar.Infrastructure.Services;

public class TvSignageService(
    AppDbContext dbContext,
    IDashboardService dashboardService,
    ITvSignageMediaStorage mediaStorage,
    ITvSignageRealtimeNotifier realtimeNotifier,
    IOptions<TvSignageSettings> tvSettings,
    ITvWeatherService weatherService,
    ITvSpeechService speechService) : ITvSignageService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<TvMonitorSummaryDto> GetMonitorSummaryAsync(CancellationToken cancellationToken = default)
    {
        var displays = await ListDisplaysAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        var callsToday = await dbContext.TvQueueCalls.CountAsync(c => c.CalledAt >= today, cancellationToken);
        var activeMedia = await dbContext.TvMedia.CountAsync(m => m.IsActive, cancellationToken);
        return new TvMonitorSummaryDto(
            displays.Count,
            displays.Count(d => d.Status == TvDisplayStatus.Online),
            displays.Count(d => d.Status == TvDisplayStatus.Offline),
            callsToday,
            activeMedia,
            displays);
    }

    public async Task<IReadOnlyList<TvDisplayDto>> ListDisplaysAsync(CancellationToken cancellationToken = default)
    {
        var displays = await dbContext.TvDisplays
            .AsNoTracking()
            .Include(d => d.Layout)
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return displays.Select(MapDisplay).ToList();
    }

    public async Task<TvDisplayDto?> GetDisplayAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var display = await dbContext.TvDisplays
            .AsNoTracking()
            .Include(d => d.Layout)
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);
        return display is null ? null : MapDisplay(display);
    }

    public async Task<TvDisplayDto> CreateDisplayAsync(CreateTvDisplayRequest request, CancellationToken cancellationToken = default)
    {
        var slug = NormalizeSlug(request.Slug);
        if (await dbContext.TvDisplays.AnyAsync(d => d.Slug == slug, cancellationToken))
        {
            throw new InvalidOperationException("Já existe uma TV com este identificador.");
        }

        var layoutId = request.LayoutId ?? await GetDefaultLayoutIdAsync(cancellationToken);
        var display = new TvDisplay
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Sector = request.Sector?.Trim(),
            LayoutId = layoutId,
            Orientation = request.Orientation,
            Resolution = request.Resolution,
            WeatherCity = request.WeatherCity?.Trim(),
            ShowPatientName = request.ShowPatientName,
            EnableSound = request.EnableSound,
            CallDisplaySeconds = request.CallDisplaySeconds <= 0 ? 30 : request.CallDisplaySeconds,
            PlayerToken = Guid.NewGuid().ToString("N"),
            Status = TvDisplayStatus.Offline,
        };

        dbContext.TvDisplays.Add(display);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(display).Reference(d => d.Layout).LoadAsync(cancellationToken);
        return MapDisplay(display);
    }

    public async Task<TvDisplayDto?> UpdateDisplayAsync(Guid id, UpdateTvDisplayRequest request, CancellationToken cancellationToken = default)
    {
        var display = await dbContext.TvDisplays.Include(d => d.Layout).FirstOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);
        if (display is null) return null;

        display.Name = request.Name.Trim();
        display.Sector = request.Sector?.Trim();
        display.LayoutId = request.LayoutId ?? display.LayoutId;
        display.Orientation = request.Orientation;
        display.Resolution = request.Resolution;
        display.WeatherCity = request.WeatherCity?.Trim();
        display.ShowPatientName = request.ShowPatientName;
        display.EnableSound = request.EnableSound;
        display.CallDisplaySeconds = request.CallDisplaySeconds <= 0 ? 30 : request.CallDisplaySeconds;

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyDisplayUpdatedAsync(display.Id, cancellationToken);
        return MapDisplay(display);
    }

    public async Task<bool> DeleteDisplayAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var display = await dbContext.TvDisplays.FirstOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);
        if (display is null) return false;
        display.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string?> RegenerateDisplayTokenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var display = await dbContext.TvDisplays.FirstOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);
        if (display is null) return null;
        display.PlayerToken = Guid.NewGuid().ToString("N");
        await dbContext.SaveChangesAsync(cancellationToken);
        return display.PlayerToken;
    }

    public async Task<IReadOnlyList<TvLayoutDto>> ListLayoutsAsync(CancellationToken cancellationToken = default)
    {
        var layouts = await dbContext.TvLayouts.AsNoTracking().Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync(cancellationToken);
        return layouts.Select(MapLayout).ToList();
    }

    public async Task<TvLayoutDto?> GetLayoutAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var layout = await dbContext.TvLayouts.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id && l.IsActive, cancellationToken);
        return layout is null ? null : MapLayout(layout);
    }

    public async Task<TvLayoutDto> CreateLayoutAsync(CreateTvLayoutRequest request, CancellationToken cancellationToken = default)
    {
        var layout = new TvLayout
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ZonesJson = SerializeZones(request.Zones),
        };
        dbContext.TvLayouts.Add(layout);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapLayout(layout);
    }

    public async Task<TvLayoutDto?> UpdateLayoutAsync(Guid id, UpdateTvLayoutRequest request, CancellationToken cancellationToken = default)
    {
        var layout = await dbContext.TvLayouts.FirstOrDefaultAsync(l => l.Id == id && l.IsActive, cancellationToken);
        if (layout is null) return null;

        layout.Name = request.Name.Trim();
        layout.Description = request.Description?.Trim();
        layout.ZonesJson = SerializeZones(request.Zones);

        await dbContext.SaveChangesAsync(cancellationToken);
        var displays = await dbContext.TvDisplays.Where(d => d.LayoutId == id && d.IsActive).Select(d => d.Id).ToListAsync(cancellationToken);
        foreach (var displayId in displays)
        {
            await realtimeNotifier.NotifyDisplayUpdatedAsync(displayId, cancellationToken);
        }

        return MapLayout(layout);
    }

    public async Task<IReadOnlyList<TvMediaDto>> ListMediaAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var media = await dbContext.TvMedia.AsNoTracking().Where(m => m.IsActive).OrderByDescending(m => m.Priority).ThenBy(m => m.Title).ToListAsync(cancellationToken);
        return media.Where(m => IsWithinSchedule(m, now)).Select(MapMedia).ToList();
    }

    public async Task<TvMediaDto> UploadMediaAsync(CreateTvMediaRequest request, string fileName, byte[] content, CancellationToken cancellationToken = default)
    {
        var media = new TvMedia
        {
            Title = request.Title.Trim(),
            MediaType = request.MediaType,
            Sector = request.Sector?.Trim(),
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Priority = request.Priority,
            DurationSeconds = request.DurationSeconds <= 0 ? 15 : request.DurationSeconds,
            MimeType = GuessMimeType(fileName),
        };
        dbContext.TvMedia.Add(media);
        await dbContext.SaveChangesAsync(cancellationToken);
        media.StoragePath = await mediaStorage.SaveAsync(media.Id, fileName, content, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyAllDisplaysAsync(cancellationToken);
        return MapMedia(media);
    }

    public async Task<bool> DeleteMediaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var media = await dbContext.TvMedia.FirstOrDefaultAsync(m => m.Id == id && m.IsActive, cancellationToken);
        if (media is null) return false;
        mediaStorage.DeleteIfExists(media.StoragePath);
        media.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyAllDisplaysAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<TvNewsDto>> ListNewsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var items = await dbContext.TvNewsItems.AsNoTracking()
            .Where(n => n.IsActive && n.PublishedAt <= now && (n.ExpiresAt == null || n.ExpiresAt >= now))
            .OrderByDescending(n => n.PublishedAt)
            .ToListAsync(cancellationToken);
        return items.Select(MapNews).ToList();
    }

    public async Task<TvNewsDto> CreateNewsAsync(CreateTvNewsRequest request, CancellationToken cancellationToken = default)
    {
        var item = new TvNewsItem
        {
            Title = request.Title.Trim(),
            Summary = request.Summary?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            Sector = request.Sector?.Trim(),
            ExpiresAt = request.ExpiresAt,
        };
        dbContext.TvNewsItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyAllDisplaysAsync(cancellationToken);
        return MapNews(item);
    }

    public async Task<bool> DeleteNewsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.TvNewsItems.FirstOrDefaultAsync(n => n.Id == id && n.IsActive, cancellationToken);
        if (item is null) return false;
        item.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyAllDisplaysAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<TvAnnouncementDto>> ListAnnouncementsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var items = await dbContext.TvAnnouncements.AsNoTracking()
            .Where(a => a.IsActive && a.StartsAt <= now && (a.EndsAt == null || a.EndsAt >= now))
            .OrderByDescending(a => a.Priority)
            .ThenByDescending(a => a.StartsAt)
            .ToListAsync(cancellationToken);
        return items.Select(MapAnnouncement).ToList();
    }

    public async Task<TvAnnouncementDto> CreateAnnouncementAsync(CreateTvAnnouncementRequest request, CancellationToken cancellationToken = default)
    {
        var item = new TvAnnouncement
        {
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Sector = request.Sector?.Trim(),
            StartsAt = request.StartsAt ?? DateTime.UtcNow,
            EndsAt = request.EndsAt,
            Priority = request.Priority,
        };
        dbContext.TvAnnouncements.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyAllDisplaysAsync(cancellationToken);
        return MapAnnouncement(item);
    }

    public async Task<bool> DeleteAnnouncementAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.TvAnnouncements.FirstOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken);
        if (item is null) return false;
        item.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyAllDisplaysAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<TvQueueCallDto>> ListRecentCallsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        var calls = await dbContext.TvQueueCalls.AsNoTracking()
            .OrderByDescending(c => c.CalledAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return calls.Select(c => MapCall(c, IsCallActive(c))).ToList();
    }

    public async Task<TvQueueCallDto> CallQueueAsync(CallTvQueueRequest request, CancellationToken cancellationToken = default)
    {
        var call = new TvQueueCall
        {
            DisplayId = request.DisplayId,
            TicketNumber = request.TicketNumber.Trim(),
            PatientName = request.PatientName?.Trim(),
            Destination = request.Destination.Trim(),
            Sector = request.Sector?.Trim(),
            KioskTicketId = request.KioskTicketId,
            ShowPatientName = request.ShowPatientName ?? false,
            DisplaySeconds = 30,
            CalledAt = DateTime.UtcNow,
        };

        if (request.DisplayId is not null)
        {
            var display = await dbContext.TvDisplays.FirstOrDefaultAsync(d => d.Id == request.DisplayId && d.IsActive, cancellationToken);
            if (display is not null)
            {
                call.DisplaySeconds = display.CallDisplaySeconds;
                call.ShowPatientName = request.ShowPatientName ?? display.ShowPatientName;
            }
        }

        dbContext.TvQueueCalls.Add(call);
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyQueueCallAsync(request.DisplayId, cancellationToken);
        return MapCall(call, true);
    }

    public async Task<TvQueueCallDto?> CallKioskTicketAsync(
        Guid kioskTicketId,
        CallKioskTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var ticket = await dbContext.KioskTickets.FirstOrDefaultAsync(t => t.Id == kioskTicketId && t.IsActive, cancellationToken);
        if (ticket is null) return null;

        ticket.Called = true;
        ticket.CalledAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CallQueueAsync(new CallTvQueueRequest(
            ticket.TicketNumber,
            ticket.PatientName,
            request.Destination,
            ticket.Sector,
            request.DisplayId,
            request.ShowPatientName,
            kioskTicketId), cancellationToken);
    }

    public async Task<IReadOnlyList<TvCampaignDto>> ListCampaignsAsync(CancellationToken cancellationToken = default)
    {
        var campaigns = await dbContext.TvCampaigns
            .AsNoTracking()
            .Include(c => c.MediaLinks)
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.Priority)
            .ToListAsync(cancellationToken);
        return campaigns.Select(MapCampaign).ToList();
    }

    public async Task<TvCampaignDto> CreateCampaignAsync(CreateTvCampaignRequest request, CancellationToken cancellationToken = default)
    {
        var campaign = new TvCampaign
        {
            Name = request.Name.Trim(),
            Sector = request.Sector?.Trim(),
            StartsAt = request.StartsAt ?? DateTime.UtcNow,
            EndsAt = request.EndsAt,
            DailyStart = ParseTime(request.DailyStart),
            DailyEnd = ParseTime(request.DailyEnd),
            DaysOfWeek = request.DaysOfWeek?.Trim(),
            Priority = request.Priority,
        };
        dbContext.TvCampaigns.Add(campaign);
        await dbContext.SaveChangesAsync(cancellationToken);
        await UpsertCampaignMediaAsync(campaign, request.MediaIds, cancellationToken);
        await NotifyAllDisplaysAsync(cancellationToken);
        await dbContext.Entry(campaign).Collection(c => c.MediaLinks).LoadAsync(cancellationToken);
        return MapCampaign(campaign);
    }

    public async Task<TvCampaignDto?> UpdateCampaignAsync(Guid id, UpdateTvCampaignRequest request, CancellationToken cancellationToken = default)
    {
        var campaign = await dbContext.TvCampaigns.Include(c => c.MediaLinks).FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);
        if (campaign is null) return null;

        campaign.Name = request.Name.Trim();
        campaign.Sector = request.Sector?.Trim();
        campaign.StartsAt = request.StartsAt ?? campaign.StartsAt;
        campaign.EndsAt = request.EndsAt;
        campaign.DailyStart = ParseTime(request.DailyStart);
        campaign.DailyEnd = ParseTime(request.DailyEnd);
        campaign.DaysOfWeek = request.DaysOfWeek?.Trim();
        campaign.Priority = request.Priority;

        await UpsertCampaignMediaAsync(campaign, request.MediaIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyAllDisplaysAsync(cancellationToken);
        return MapCampaign(campaign);
    }

    public async Task<bool> DeleteCampaignAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var campaign = await dbContext.TvCampaigns.FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);
        if (campaign is null) return false;
        campaign.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyAllDisplaysAsync(cancellationToken);
        return true;
    }

    public string GetSpeechProvider() => speechService.Provider;

    public async Task<byte[]?> GetCallSpeechAsync(string slug, string token, Guid callId, CancellationToken cancellationToken = default)
    {
        var display = await dbContext.TvDisplays.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Slug == slug && d.PlayerToken == token && d.IsActive, cancellationToken);
        if (display is null || !display.EnableSound) return null;

        var call = await dbContext.TvQueueCalls.AsNoTracking().FirstOrDefaultAsync(c => c.Id == callId, cancellationToken);
        if (call is null || !IsCallActive(call)) return null;

        var text = call.ShowPatientName && !string.IsNullOrWhiteSpace(call.PatientName)
            ? TvCallSpeech.FormatPatientCall(call.PatientName, call.Destination)
            : $"Senha {call.TicketNumber}, dirija-se a {call.Destination}";

        return await speechService.SynthesizeAsync(text, cancellationToken);
    }

    public async Task<TvPlayerStateDto?> GetPlayerStateAsync(string slug, string token, CancellationToken cancellationToken = default)
    {
        var display = await dbContext.TvDisplays
            .AsNoTracking()
            .Include(d => d.Layout)
            .FirstOrDefaultAsync(d => d.Slug == slug && d.PlayerToken == token && d.IsActive, cancellationToken);
        if (display is null || display.Layout is null) return null;

        var now = DateTime.UtcNow;
        var media = await LoadMediaForDisplayAsync(display, now, cancellationToken);
        var news = await LoadNewsForDisplayAsync(display, cancellationToken);
        var announcements = await LoadAnnouncementsForDisplayAsync(display, cancellationToken);
        var calls = await LoadCallsForDisplayAsync(display, cancellationToken);
        var activeCall = calls.FirstOrDefault(c => c.IsActive);
        var weather = await LoadWeatherAsync(display.WeatherCity, cancellationToken);
        var dashboard = await LoadDashboardAsync(cancellationToken);
        var schedule = await LoadScheduleAsync(display.Sector, cancellationToken);

        string? speechUrl = null;
        if (activeCall is not null && display.EnableSound && !string.Equals(speechService.Provider, "Browser", StringComparison.OrdinalIgnoreCase))
        {
            speechUrl = $"/api/tv-signage/player/{slug}/speech/{activeCall.Id}?token={token}";
        }

        return new TvPlayerStateDto(
            MapDisplay(display),
            MapLayout(display.Layout),
            media,
            news,
            announcements,
            calls,
            activeCall,
            weather,
            dashboard,
            schedule,
            speechService.Provider,
            speechUrl,
            DateTime.UtcNow);
    }

    public async Task<bool> RegisterHeartbeatAsync(string slug, string token, TvHeartbeatRequest request, CancellationToken cancellationToken = default)
    {
        var display = await dbContext.TvDisplays.FirstOrDefaultAsync(d => d.Slug == slug && d.PlayerToken == token && d.IsActive, cancellationToken);
        if (display is null) return false;

        display.LastSeenAt = DateTime.UtcNow;
        display.Status = TvDisplayStatus.Online;
        if (!string.IsNullOrWhiteSpace(request.IpAddress)) display.IpAddress = request.IpAddress.Trim();
        if (!string.IsNullOrWhiteSpace(request.Resolution)) display.Resolution = request.Resolution.Trim();

        dbContext.TvDisplayLogs.Add(new TvDisplayLog
        {
            DisplayId = display.Id,
            EventType = "heartbeat",
            Message = "Player online",
            OccurredAt = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task NotifyAllDisplaysAsync(CancellationToken cancellationToken)
    {
        var ids = await dbContext.TvDisplays.Where(d => d.IsActive).Select(d => d.Id).ToListAsync(cancellationToken);
        foreach (var id in ids)
        {
            await realtimeNotifier.NotifyDisplayUpdatedAsync(id, cancellationToken);
        }
    }

    private async Task<Guid?> GetDefaultLayoutIdAsync(CancellationToken cancellationToken)
        => await dbContext.TvLayouts.Where(l => l.IsActive).OrderBy(l => l.IsSystem ? 0 : 1).Select(l => l.Id).FirstOrDefaultAsync(cancellationToken);

    private async Task<IReadOnlyList<TvMediaDto>> LoadMediaForDisplayAsync(TvDisplay display, DateTime now, CancellationToken cancellationToken)
    {
        var baseMedia = await dbContext.TvMedia.AsNoTracking().Where(m => m.IsActive).ToListAsync(cancellationToken);
        var campaignMedia = await LoadActiveCampaignMediaAsync(display.Sector, now, cancellationToken);

        var merged = new List<TvMedia>();
        merged.AddRange(campaignMedia);
        foreach (var item in baseMedia)
        {
            if (merged.Any(m => m.Id == item.Id)) continue;
            if (!string.IsNullOrWhiteSpace(display.Sector) && item.Sector is not null && item.Sector != display.Sector) continue;
            if (!IsWithinSchedule(item, now)) continue;
            merged.Add(item);
        }

        return merged.OrderByDescending(m => m.Priority).ThenBy(m => m.Title).Select(MapMedia).ToList();
    }

    private async Task<List<TvMedia>> LoadActiveCampaignMediaAsync(string? sector, DateTime now, CancellationToken cancellationToken)
    {
        var campaigns = await dbContext.TvCampaigns
            .AsNoTracking()
            .Include(c => c.MediaLinks).ThenInclude(l => l.Media)
            .Where(c => c.IsActive && c.StartsAt <= now && (c.EndsAt == null || c.EndsAt >= now))
            .ToListAsync(cancellationToken);

        var result = new List<TvMedia>();
        foreach (var campaign in campaigns.Where(c => IsCampaignActiveNow(c, now)))
        {
            if (!string.IsNullOrWhiteSpace(sector) && campaign.Sector is not null && campaign.Sector != sector) continue;
            foreach (var link in campaign.MediaLinks.Where(l => l.Media.IsActive).OrderBy(l => l.SortOrder))
            {
                if (result.All(m => m.Id != link.Media.Id))
                {
                    result.Add(link.Media);
                }
            }
        }

        return result.OrderByDescending(m => m.Priority).ToList();
    }

    private async Task<IReadOnlyList<TvNewsDto>> LoadNewsForDisplayAsync(TvDisplay display, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = dbContext.TvNewsItems.AsNoTracking()
            .Where(n => n.IsActive && n.PublishedAt <= now && (n.ExpiresAt == null || n.ExpiresAt >= now));
        if (!string.IsNullOrWhiteSpace(display.Sector))
        {
            query = query.Where(n => n.Sector == null || n.Sector == display.Sector);
        }

        var items = await query.OrderByDescending(n => n.PublishedAt).Take(20).ToListAsync(cancellationToken);
        return items.Select(MapNews).ToList();
    }

    private async Task<IReadOnlyList<TvAnnouncementDto>> LoadAnnouncementsForDisplayAsync(TvDisplay display, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = dbContext.TvAnnouncements.AsNoTracking()
            .Where(a => a.IsActive && a.StartsAt <= now && (a.EndsAt == null || a.EndsAt >= now));
        if (!string.IsNullOrWhiteSpace(display.Sector))
        {
            query = query.Where(a => a.Sector == null || a.Sector == display.Sector);
        }

        var items = await query.OrderByDescending(a => a.Priority).Take(10).ToListAsync(cancellationToken);
        return items.Select(MapAnnouncement).ToList();
    }

    private async Task<IReadOnlyList<TvQueueCallDto>> LoadCallsForDisplayAsync(TvDisplay display, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddHours(-2);
        var query = dbContext.TvQueueCalls.AsNoTracking().Where(c => c.CalledAt >= since);
        query = query.Where(c => c.DisplayId == null || c.DisplayId == display.Id);
        var calls = await query.OrderByDescending(c => c.CalledAt).Take(8).ToListAsync(cancellationToken);
        return calls.Select(c => MapCall(c, IsCallActive(c))).ToList();
    }

    private async Task<TvWeatherDto?> LoadWeatherAsync(string? city, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(city)) return null;
        var snapshot = await weatherService.EnsureFreshAsync(city, cancellationToken);
        return snapshot is null ? null : new TvWeatherDto(snapshot.City, snapshot.TemperatureC, snapshot.Condition, snapshot.Icon, snapshot.HumidityPercent, snapshot.UpdatedAt);
    }

    private async Task<IReadOnlyList<TvScheduleItemDto>> LoadScheduleAsync(string? sector, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);

        var professionals = await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Professional).ThenInclude(p => p.Specialty)
            .Where(a => a.IsActive && a.ScheduledAt >= start && a.ScheduledAt < end)
            .GroupBy(a => a.ProfessionalId)
            .Select(g => new TvScheduleItemDto(
                g.First().Professional.FullName,
                g.First().Professional.Specialty.Name,
                "Agenda do dia",
                g.Min(a => a.ScheduledAt).ToString("HH:mm")))
            .Take(8)
            .ToListAsync(cancellationToken);

        var shifts = await dbContext.EmployeeShifts
            .AsNoTracking()
            .Include(s => s.Employee)
            .Include(s => s.Department)
            .Where(s => s.IsActive && s.ShiftDate == today)
            .Select(s => new TvScheduleItemDto(
                s.Employee.FullName,
                s.Employee.JobTitle ?? s.Employee.Role.ToString(),
                s.Department.Name,
                s.ShiftType.ToString()))
            .Take(6)
            .ToListAsync(cancellationToken);

        return professionals.Concat(shifts).Take(12).ToList();
    }

    private async Task<TvDashboardWidgetDto?> LoadDashboardAsync(CancellationToken cancellationToken)
    {
        var dash = await dashboardService.GetOperationalDashboardAsync(null, cancellationToken: cancellationToken);
        return new TvDashboardWidgetDto(
            dash.AttendancesToday,
            dash.EmergencyWaiting,
            dash.AverageEmergencyWaitMinutes,
            dash.BedOccupancyRate,
            dash.LabOrdersPending);
    }

    private static bool IsCallActive(TvQueueCall call)
        => DateTime.UtcNow <= call.CalledAt.AddSeconds(call.DisplaySeconds);

    private static bool IsWithinSchedule(TvMedia media, DateTime now)
        => (media.StartsAt is null || media.StartsAt <= now) && (media.EndsAt is null || media.EndsAt >= now);

    private TvDisplayDto MapDisplay(TvDisplay display)
    {
        var baseUrl = tvSettings.Value.PlayerBaseUrl.TrimEnd('/');
        var playerUrl = $"{baseUrl}/tv/{display.Slug}?token={display.PlayerToken}";
        return new TvDisplayDto(
            display.Id,
            display.Name,
            display.Slug,
            display.Sector,
            display.IpAddress,
            display.Resolution,
            display.Orientation,
            display.Status,
            display.PlayerToken,
            display.LayoutId,
            display.Layout?.Name,
            display.ShowPatientName,
            display.EnableSound,
            display.CallDisplaySeconds,
            display.WeatherCity,
            display.LastSeenAt,
            playerUrl);
    }

    private static TvLayoutDto MapLayout(TvLayout layout)
        => new(layout.Id, layout.Name, layout.Description, DeserializeZones(layout.ZonesJson), layout.IsSystem);

    private static TvMediaDto MapMedia(TvMedia media)
        => new(media.Id, media.Title, media.MediaType, "/" + media.StoragePath.TrimStart('/'), media.MimeType, media.Sector, media.StartsAt, media.EndsAt, media.Priority, media.DurationSeconds);

    private static TvNewsDto MapNews(TvNewsItem item)
        => new(item.Id, item.Title, item.Summary, item.ImageUrl, item.Sector, item.PublishedAt, item.ExpiresAt);

    private static TvAnnouncementDto MapAnnouncement(TvAnnouncement item)
        => new(item.Id, item.Title, item.Body, item.Sector, item.StartsAt, item.EndsAt, item.Priority);

    private static TvQueueCallDto MapCall(TvQueueCall call, bool isActive)
        => new(call.Id, call.TicketNumber, call.ShowPatientName ? call.PatientName : null, call.Destination, call.Sector, call.CalledAt, call.DisplaySeconds, call.ShowPatientName, isActive);

    private static TvCampaignDto MapCampaign(TvCampaign campaign)
        => new(
            campaign.Id,
            campaign.Name,
            campaign.Sector,
            campaign.StartsAt,
            campaign.EndsAt,
            campaign.DailyStart?.ToString("HH:mm"),
            campaign.DailyEnd?.ToString("HH:mm"),
            campaign.DaysOfWeek,
            campaign.Priority,
            campaign.MediaLinks.OrderBy(l => l.SortOrder).Select(l => l.MediaId).ToList());

    private static bool IsCampaignActiveNow(TvCampaign campaign, DateTime now)
    {
        if (campaign.StartsAt > now || (campaign.EndsAt is not null && campaign.EndsAt < now)) return false;

        if (!string.IsNullOrWhiteSpace(campaign.DaysOfWeek))
        {
            var day = (int)now.DayOfWeek;
            var allowed = campaign.DaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var d) ? d : -1);
            if (!allowed.Contains(day)) return false;
        }

        var localTime = TimeOnly.FromDateTime(now);
        if (campaign.DailyStart is not null && localTime < campaign.DailyStart.Value) return false;
        if (campaign.DailyEnd is not null && localTime > campaign.DailyEnd.Value) return false;
        return true;
    }

    private async Task UpsertCampaignMediaAsync(TvCampaign campaign, IReadOnlyList<Guid> mediaIds, CancellationToken cancellationToken)
    {
        var existing = await dbContext.TvCampaignMedia.Where(l => l.CampaignId == campaign.Id).ToListAsync(cancellationToken);
        dbContext.TvCampaignMedia.RemoveRange(existing);

        var order = 0;
        foreach (var mediaId in mediaIds.Distinct())
        {
            dbContext.TvCampaignMedia.Add(new TvCampaignMedia
            {
                CampaignId = campaign.Id,
                MediaId = mediaId,
                SortOrder = order++,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static TimeOnly? ParseTime(string? value)
        => TimeOnly.TryParse(value, out var time) ? time : null;

    private static string NormalizeSlug(string slug)
        => slug.Trim().ToLowerInvariant().Replace(' ', '-');

    private static string SerializeZones(IReadOnlyList<TvLayoutZoneDto> zones)
        => JsonSerializer.Serialize(zones, JsonOptions);

    private static IReadOnlyList<TvLayoutZoneDto> DeserializeZones(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<TvLayoutZoneDto>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string? GuessMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".pdf" => "application/pdf",
            _ => null,
        };
    }
}
