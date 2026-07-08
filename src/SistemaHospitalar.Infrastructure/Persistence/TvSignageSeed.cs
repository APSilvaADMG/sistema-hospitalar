using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.TvSignage;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class TvSignageSeed
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await EnsureLayoutsAsync(db, cancellationToken);
        await EnsureHospitalSghLayoutAsync(db, cancellationToken);
        await EnsureDisplaysAsync(db, cancellationToken);
        await EnsureNewsAsync(db, cancellationToken);
        await EnsureAnnouncementsAsync(db, cancellationToken);
        await EnsureWeatherAsync(db, cancellationToken);
        await EnsureCampaignsAsync(db, cancellationToken);
        await EnsureDemoContentAsync(db, cancellationToken);
        await EnsureWaitingRoomDisplayDefaultsAsync(db, cancellationToken);
    }

    private static async Task EnsureLayoutsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.TvLayouts.AnyAsync(cancellationToken)) return;

        var layouts = new (string Name, string Description, TvLayoutZoneDto[] Zones)[]
        {
            (
                "Recepção — Vídeo + Fila + Clima",
                "Área principal de mídia, chamadas laterais e rodapé informativo.",
                [
                    new("main", TvWidgetType.MediaCarousel, 0, 0, 68, 72),
                    new("queue", TvWidgetType.QueueCalls, 68, 0, 32, 42),
                    new("weather", TvWidgetType.Weather, 68, 42, 32, 30),
                    new("ticker", TvWidgetType.NewsTicker, 0, 72, 100, 28),
                ]),
            (
                "Ambulatório — Chamada em destaque",
                "Chamada de senha no topo e vídeo institucional abaixo.",
                [
                    new("queue", TvWidgetType.QueueCalls, 0, 0, 100, 28),
                    new("main", TvWidgetType.MediaCarousel, 0, 28, 100, 52),
                    new("dashboard", TvWidgetType.Dashboard, 0, 80, 50, 20),
                    new("clock", TvWidgetType.Clock, 50, 80, 50, 20),
                ]),
            (
                "Corredor — Informativo completo",
                "Clima, vídeo, notícias e indicadores hospitalares.",
                [
                    new("weather", TvWidgetType.Weather, 0, 0, 22, 30),
                    new("main", TvWidgetType.MediaCarousel, 22, 0, 78, 58),
                    new("announcements", TvWidgetType.Announcements, 0, 30, 22, 70),
                    new("ticker", TvWidgetType.NewsTicker, 22, 58, 78, 42),
                ]),
        };

        foreach (var (name, description, zones) in layouts)
        {
            db.TvLayouts.Add(new TvLayout
            {
                Name = name,
                Description = description,
                ZonesJson = JsonSerializer.Serialize(zones, JsonOptions),
                IsSystem = true,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureHospitalSghLayoutAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        const string layoutName = "Sala de Espera — SGH";
        var zones = new[]
        {
            new TvLayoutZoneDto("sgh-template", TvWidgetType.QueueCalls, 0, 0, 100, 100),
        };
        var zonesJson = JsonSerializer.Serialize(zones, JsonOptions);

        var layout = await db.TvLayouts.FirstOrDefaultAsync(l => l.Name == layoutName, cancellationToken);
        if (layout is null)
        {
            layout = new TvLayout
            {
                Name = layoutName,
                Description = "Layout institucional SGH — chamadas, publicidade e comunicados (referência LayoutTVHospital).",
                ZonesJson = zonesJson,
                IsSystem = true,
            };
            db.TvLayouts.Add(layout);
        }
        else
        {
            layout.Description = "Layout institucional SGH — chamadas, publicidade e comunicados (referência LayoutTVHospital).";
            layout.ZonesJson = zonesJson;
            layout.IsSystem = true;
        }

        await db.SaveChangesAsync(cancellationToken);

        // Salas de espera: sempre usam o layout institucional SGH
        var displays = await db.TvDisplays
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken);

        var waitingRoomDisplays = displays.Where(IsWaitingRoomDisplay).ToList();
        foreach (var display in waitingRoomDisplays)
        {
            display.LayoutId = layout.Id;
        }

        if (waitingRoomDisplays.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool IsWaitingRoomDisplay(TvDisplay display)
    {
        var waitingSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "recepcao",
            "ambulatorio",
            "laboratorio",
            "sala-espera",
            "espera",
            "sala-de-casa",
            "sala-casa",
        };

        if (waitingSlugs.Contains(display.Slug))
        {
            return true;
        }

        if (System.Net.IPAddress.TryParse(display.Slug, out _))
        {
            return true;
        }

        var label = $"{display.Sector} {display.Name}".ToLowerInvariant();
        return label.Contains("recep", StringComparison.Ordinal)
            || label.Contains("espera", StringComparison.Ordinal)
            || label.Contains("ambulat", StringComparison.Ordinal)
            || label.Contains("sala", StringComparison.Ordinal)
            || label.Contains("casa", StringComparison.Ordinal);
    }

    private static async Task EnsureDisplaysAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.TvDisplays.AnyAsync(cancellationToken)) return;

        var layouts = await db.TvLayouts.OrderBy(l => l.Name).ToListAsync(cancellationToken);
        var sghLayout = layouts.FirstOrDefault(l => l.Name == "Sala de Espera — SGH") ?? layouts.First();

        var displays = new (string Name, string Slug, string Sector, Guid LayoutId, string City)[]
        {
            ("TV Recepção Principal", "recepcao", "Recepção", sghLayout.Id, "Arapiraca - AL"),
            ("TV Ambulatório", "ambulatorio", "Ambulatório", sghLayout.Id, "Arapiraca - AL"),
            ("TV Laboratório", "laboratorio", "Laboratório", sghLayout.Id, "Arapiraca - AL"),
            ("TV UTI", "uti", "UTI Adulto", sghLayout.Id, "Arapiraca - AL"),
        };

        foreach (var (name, slug, sector, layoutId, city) in displays)
        {
            db.TvDisplays.Add(new TvDisplay
            {
                Name = name,
                Slug = slug,
                Sector = sector,
                LayoutId = layoutId,
                WeatherCity = city,
                PlayerToken = Guid.NewGuid().ToString("N"),
                ShowPatientName = true,
                EnableSound = true,
                CallDisplaySeconds = 30,
                Status = TvDisplayStatus.Offline,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureNewsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.TvNewsItems.AnyAsync(cancellationToken)) return;

        db.TvNewsItems.AddRange(
            new TvNewsItem
            {
                Title = "Campanha Outubro Rosa",
                Summary = "Agende sua mamografia. Prevenção salva vidas.",
                Sector = null,
            },
            new TvNewsItem
            {
                Title = "Vacinação em dia",
                Summary = "Confira o calendário vacinal na recepção.",
                Sector = "Recepção",
            },
            new TvNewsItem
            {
                Title = "Treinamento de segurança do paciente",
                Summary = "Dias 24 e 25 — Sala de Reuniões, 4º andar.",
                Sector = null,
            });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAnnouncementsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.TvAnnouncements.AnyAsync(cancellationToken)) return;

        db.TvAnnouncements.Add(new TvAnnouncement
        {
            Title = "Aviso Importante",
            Body = "Mantenha seus documentos em mãos para agilizar o atendimento.",
            Priority = 1,
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureWeatherAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.TvWeatherSnapshots.AnyAsync(cancellationToken)) return;

        db.TvWeatherSnapshots.Add(new TvWeatherSnapshot
        {
            City = "Arapiraca - AL",
            TemperatureC = 29,
            Condition = "Ensolarado",
            Icon = "☀️",
            HumidityPercent = 62,
            UpdatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCampaignsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.TvCampaigns.AnyAsync(cancellationToken)) return;

        var campaign = new TvCampaign
        {
            Name = "Campanha institucional — horário comercial",
            Sector = null,
            StartsAt = DateTime.UtcNow.Date,
            EndsAt = DateTime.UtcNow.Date.AddMonths(3),
            DailyStart = new TimeOnly(8, 0),
            DailyEnd = new TimeOnly(18, 0),
            DaysOfWeek = "1,2,3,4,5",
            Priority = 10,
        };
        db.TvCampaigns.Add(campaign);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Conteúdo de demonstração hospitalar — idempotente, seguro para reexecutar.</summary>
    private static async Task EnsureDemoContentAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        await EnsureDemoDisplaysAsync(db, cancellationToken);
        await EnsureDemoMediaAsync(db, cancellationToken);
        await EnsureDemoCampaignLinksAsync(db, cancellationToken);
        await EnsureDemoNewsAsync(db, cancellationToken);
        await EnsureDemoAnnouncementsAsync(db, cancellationToken);
        await EnsureDemoQueueCallsAsync(db, cancellationToken);
    }

    private static async Task EnsureDemoDisplaysAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var sghLayout = await db.TvLayouts
            .FirstOrDefaultAsync(l => l.Name == "Sala de Espera — SGH" && l.IsActive, cancellationToken);
        if (sghLayout is null) return;

        var demoDisplays = new (string Name, string Slug, string Sector, string? IpAddress)[]
        {
            ("TV Sala de Casa", "sala-de-casa", "Sala de Casa", "192.168.3.33"),
        };

        foreach (var (name, slug, sector, ipAddress) in demoDisplays)
        {
            var display = await db.TvDisplays.FirstOrDefaultAsync(d => d.Slug == slug, cancellationToken);
            if (display is null)
            {
                db.TvDisplays.Add(new TvDisplay
                {
                    Name = name,
                    Slug = slug,
                    Sector = sector,
                    LayoutId = sghLayout.Id,
                    IpAddress = ipAddress,
                    WeatherCity = "Arapiraca - AL",
                    PlayerToken = Guid.NewGuid().ToString("N"),
                    ShowPatientName = true,
                    EnableSound = true,
                    CallDisplaySeconds = 30,
                    Status = TvDisplayStatus.Offline,
                });
            }
            else
            {
                display.Name = name;
                display.Sector = sector;
                display.LayoutId = sghLayout.Id;
                if (!string.IsNullOrWhiteSpace(ipAddress))
                {
                    display.IpAddress = ipAddress;
                }
            }
        }

        var ipDisplay = await db.TvDisplays
            .FirstOrDefaultAsync(d => d.Slug == "192.168.3.33" || d.IpAddress == "192.168.3.33", cancellationToken);
        if (ipDisplay is not null)
        {
            ipDisplay.LayoutId = sghLayout.Id;
            ipDisplay.Sector ??= "Sala de Casa";
            ipDisplay.IpAddress ??= "192.168.3.33";
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDemoMediaAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var demoMedia = new (string Title, TvMediaType Type, string Path, string? Sector, int Priority, int Duration)[]
        {
            ("Campanha Outubro Rosa", TvMediaType.Image, "tv-demo/outubro-rosa.svg", null, 20, 12),
            ("Vacinação Influenza 2026", TvMediaType.Image, "tv-demo/vacinacao.svg", "Recepção", 18, 12),
            ("Check-up Executivo SGH", TvMediaType.Image, "tv-demo/checkup.svg", null, 15, 10),
            ("Humanização no Atendimento", TvMediaType.Image, "tv-demo/humanizacao.svg", null, 12, 10),
            ("Laboratório — orientações de jejum", TvMediaType.Image, "tv-demo/laboratorio-jejum.svg", "Laboratório", 16, 12),
        };

        foreach (var (title, type, path, sector, priority, duration) in demoMedia)
        {
            var media = await db.TvMedia.FirstOrDefaultAsync(m => m.Title == title, cancellationToken);
            if (media is null)
            {
                db.TvMedia.Add(new TvMedia
                {
                    Title = title,
                    MediaType = type,
                    StoragePath = path,
                    MimeType = "image/svg+xml",
                    Sector = sector,
                    Priority = priority,
                    DurationSeconds = duration,
                    StartsAt = now.AddMonths(-1),
                    EndsAt = now.AddMonths(6),
                });
            }
            else
            {
                media.MediaType = type;
                media.StoragePath = path;
                media.MimeType = "image/svg+xml";
                media.Sector = sector;
                media.Priority = priority;
                media.DurationSeconds = duration;
                media.StartsAt = now.AddMonths(-1);
                media.EndsAt = now.AddMonths(6);
                media.IsActive = true;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDemoCampaignLinksAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        const string campaignName = "Campanha institucional — horário comercial";
        var campaign = await db.TvCampaigns
            .Include(c => c.MediaLinks)
            .FirstOrDefaultAsync(c => c.Name == campaignName, cancellationToken);

        if (campaign is null)
        {
            campaign = new TvCampaign
            {
                Name = campaignName,
                Sector = null,
                StartsAt = DateTime.UtcNow.Date.AddMonths(-1),
                EndsAt = DateTime.UtcNow.Date.AddMonths(6),
                DailyStart = new TimeOnly(0, 0),
                DailyEnd = new TimeOnly(23, 59),
                DaysOfWeek = "0,1,2,3,4,5,6",
                Priority = 10,
            };
            db.TvCampaigns.Add(campaign);
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            campaign.EndsAt = DateTime.UtcNow.Date.AddMonths(6);
            campaign.DailyStart = new TimeOnly(0, 0);
            campaign.DailyEnd = new TimeOnly(23, 59);
            campaign.DaysOfWeek = "0,1,2,3,4,5,6";
        }

        var mediaTitles = new[]
        {
            "Campanha Outubro Rosa",
            "Vacinação Influenza 2026",
            "Check-up Executivo SGH",
            "Humanização no Atendimento",
            "Laboratório — orientações de jejum",
        };

        var mediaIds = await db.TvMedia
            .Where(m => m.IsActive && mediaTitles.Contains(m.Title))
            .OrderByDescending(m => m.Priority)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var existingLinks = campaign.MediaLinks.ToList();
        var order = 0;
        foreach (var mediaId in mediaIds)
        {
            if (existingLinks.Any(l => l.MediaId == mediaId)) continue;
            db.TvCampaignMedia.Add(new TvCampaignMedia
            {
                CampaignId = campaign.Id,
                MediaId = mediaId,
                SortOrder = order++,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDemoNewsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var items = new (string Title, string Summary, string? Sector)[]
        {
            ("Horário ampliado no Ambulatório", "Atendimento estendido até 19h de segunda a quinta.", "Ambulatório"),
            ("Novo tomógrafo em operação", "Exames de imagem com menor tempo de espera.", null),
            ("Campanha de doação de sangue", "Doe sangue — estoque em alerta. Hemocentro no 2º andar.", "Recepção"),
            ("Higienização das mãos", "Lembrete: higienize as mãos ao entrar nas áreas assistenciais.", null),
            ("Programa de humanização", "Equipe SGH reforça acolhimento e escuta ativa.", null),
        };

        foreach (var (title, summary, sector) in items)
        {
            if (await db.TvNewsItems.AnyAsync(n => n.Title == title, cancellationToken)) continue;
            db.TvNewsItems.Add(new TvNewsItem
            {
                Title = title,
                Summary = summary,
                Sector = sector,
                PublishedAt = now.AddDays(-1),
                ExpiresAt = now.AddMonths(3),
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDemoAnnouncementsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var items = new (string Title, string Body, string? Sector, int Priority)[]
        {
            (
                "Documentos para atendimento",
                "Apresente RG, CPF, carteirinha do convênio e pedidos médicos na recepção.",
                "Recepção",
                10),
            (
                "Uso obrigatório de máscara",
                "Em áreas de maior fluxo, utilize máscara facial. Dispenser na entrada principal.",
                null,
                8),
            (
                "Ambulatório — retirada de exames",
                "Resultados disponíveis no portal do paciente ou balcão do laboratório após 48h.",
                "Ambulatório",
                7),
            (
                "Laboratório — coleta por ordem de chegada",
                "Senhas liberadas às 6h30. Prioridade conforme legislação vigente.",
                "Laboratório",
                9),
            (
                "Sala de Casa — visitas",
                "Horário de visitas: 14h às 17h. Máximo 2 acompanhantes por paciente.",
                "Sala de Casa",
                6),
            (
                "Wi-Fi para acompanhantes",
                "Rede SGH-Visitantes disponível na recepção. Senha no balcão de informações.",
                null,
                5),
        };

        foreach (var (title, body, sector, priority) in items)
        {
            var existing = await db.TvAnnouncements.FirstOrDefaultAsync(a => a.Title == title, cancellationToken);
            if (existing is null)
            {
                db.TvAnnouncements.Add(new TvAnnouncement
                {
                    Title = title,
                    Body = body,
                    Sector = sector,
                    StartsAt = now.AddDays(-7),
                    EndsAt = now.AddMonths(3),
                    Priority = priority,
                });
            }
            else
            {
                existing.Body = body;
                existing.Sector = sector;
                existing.Priority = priority;
                existing.StartsAt = now.AddDays(-7);
                existing.EndsAt = now.AddMonths(3);
                existing.IsActive = true;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDemoQueueCallsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var displays = await db.TvDisplays
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken);

        var displayBySlug = displays.ToDictionary(d => d.Slug, d => d.Id, StringComparer.OrdinalIgnoreCase);
        Guid? ResolveDisplayId(string? displaySlug)
        {
            if (string.IsNullOrWhiteSpace(displaySlug)) return null;
            if (displayBySlug.TryGetValue(displaySlug, out var id)) return id;
            var byIp = displays.FirstOrDefault(d =>
                string.Equals(d.IpAddress, displaySlug, StringComparison.OrdinalIgnoreCase));
            return byIp?.Id;
        }

        var now = DateTime.UtcNow;
        var demoCalls = new (string Ticket, string? Patient, string Destination, string? Sector, string? DisplaySlug, int SecondsAgo, bool ShowName)[]
        {
            ("R048", null, "Guichê 3", "Recepção", "recepcao", 8, false),
            ("R047", null, "Guichê 2", "Recepção", "recepcao", 180, false),
            ("R046", "Maria S.", "Guichê 1", "Recepção", "recepcao", 480, true),
            ("R045", null, "Guichê 4", "Recepção", null, 900, false),
            ("A023", null, "Sala Consultório 102", "Ambulatório", "ambulatorio", 300, false),
            ("A022", "João P.", "Sala Consultório 101", "Ambulatório", "ambulatorio", 720, true),
            ("A021", null, "Guichê Ambulatório", "Ambulatório", "ambulatorio", 1200, false),
            ("L015", null, "Guichê Laboratório", "Laboratório", "laboratorio", 240, false),
            ("L014", "Ana C.", "Sala Coleta 2", "Laboratório", "laboratorio", 600, true),
            ("C003", null, "Sala de Casa — Enfermaria", "Sala de Casa", "sala-de-casa", 420, false),
            ("C002", "Pedro M.", "Guichê Recepção Casa", "Sala de Casa", "sala-de-casa", 960, true),
            ("C004", null, "Sala Enfermaria 12", "Sala de Casa", "192.168.3.33", 360, false),
        };

        foreach (var (ticket, patient, destination, sector, displaySlug, secondsAgo, showName) in demoCalls)
        {
            var displayId = ResolveDisplayId(displaySlug);

            var calledAt = now.AddSeconds(-secondsAgo);
            var call = await db.TvQueueCalls
                .FirstOrDefaultAsync(c => c.TicketNumber == ticket && c.Destination == destination, cancellationToken);

            if (call is null)
            {
                db.TvQueueCalls.Add(new TvQueueCall
                {
                    DisplayId = displayId,
                    TicketNumber = ticket,
                    PatientName = patient,
                    Destination = destination,
                    Sector = sector,
                    CalledAt = calledAt,
                    DisplaySeconds = 30,
                    ShowPatientName = showName,
                });
            }
            else
            {
                call.DisplayId = displayId;
                call.PatientName = patient;
                call.Sector = sector;
                call.CalledAt = calledAt;
                call.DisplaySeconds = 30;
                call.ShowPatientName = showName;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Garante nome do paciente e som nas TVs de sala de espera já existentes.</summary>
    private static async Task EnsureWaitingRoomDisplayDefaultsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var displays = await db.TvDisplays
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var display in displays.Where(IsWaitingRoomDisplay))
        {
            if (!display.ShowPatientName)
            {
                display.ShowPatientName = true;
                changed = true;
            }

            if (!display.EnableSound)
            {
                display.EnableSound = true;
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
