using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

/// <summary>TV corporativa cadastrada na instituição.</summary>
public class TvDisplay : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string? IpAddress { get; set; }
    public string? Resolution { get; set; }
    public TvDisplayOrientation Orientation { get; set; } = TvDisplayOrientation.Horizontal;
    public TvDisplayStatus Status { get; set; } = TvDisplayStatus.Offline;
    public string PlayerToken { get; set; } = string.Empty;
    public Guid? LayoutId { get; set; }
    public TvLayout? Layout { get; set; }
    public bool ShowPatientName { get; set; }
    public bool EnableSound { get; set; } = true;
    public int CallDisplaySeconds { get; set; } = 30;
    public string? WeatherCity { get; set; }
    public DateTime? LastSeenAt { get; set; }
}

/// <summary>Layout modular com zonas (JSON).</summary>
public class TvLayout : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ZonesJson { get; set; } = "[]";
    public bool IsSystem { get; set; }
}

/// <summary>Mídia para campanhas e playlists.</summary>
public class TvMedia : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public TvMediaType MediaType { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public string? Sector { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int Priority { get; set; }
    public int DurationSeconds { get; set; } = 15;
}

/// <summary>Playlist de mídias vinculada a uma TV.</summary>
public class TvPlaylist : BaseEntity
{
    public Guid DisplayId { get; set; }
    public TvDisplay Display { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public ICollection<TvPlaylistItem> Items { get; set; } = [];
}

public class TvPlaylistItem : BaseEntity
{
    public Guid PlaylistId { get; set; }
    public TvPlaylist Playlist { get; set; } = null!;
    public Guid MediaId { get; set; }
    public TvMedia Media { get; set; } = null!;
    public int SortOrder { get; set; }
}

/// <summary>Notícia institucional para TVs.</summary>
public class TvNewsItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? ImageUrl { get; set; }
    public string? Sector { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>Aviso / mural corporativo para TVs.</summary>
public class TvAnnouncement : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public DateTime StartsAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndsAt { get; set; }
    public int Priority { get; set; }
}

/// <summary>Chamada de senha exibida nas TVs.</summary>
public class TvQueueCall : BaseEntity
{
    public Guid? DisplayId { get; set; }
    public TvDisplay? Display { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string Destination { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public Guid? KioskTicketId { get; set; }
    public DateTime CalledAt { get; set; } = DateTime.UtcNow;
    public int DisplaySeconds { get; set; } = 30;
    public bool ShowPatientName { get; set; }
}

/// <summary>Cache de previsão do tempo.</summary>
public class TvWeatherSnapshot : BaseEntity
{
    public string City { get; set; } = string.Empty;
    public decimal TemperatureC { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int HumidityPercent { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Campanha programada por período e horário.</summary>
public class TvCampaign : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public DateTime StartsAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndsAt { get; set; }
    /// <summary>Horário diário inicial (UTC ou local conforme parametrização).</summary>
    public TimeOnly? DailyStart { get; set; }
    public TimeOnly? DailyEnd { get; set; }
    /// <summary>Dias da semana: 0=Domingo ... 6=Sábado, separados por vírgula.</summary>
    public string? DaysOfWeek { get; set; }
    public int Priority { get; set; }
    public ICollection<TvCampaignMedia> MediaLinks { get; set; } = [];
}

public class TvCampaignMedia : BaseEntity
{
    public Guid CampaignId { get; set; }
    public TvCampaign Campaign { get; set; } = null!;
    public Guid MediaId { get; set; }
    public TvMedia Media { get; set; } = null!;
    public int SortOrder { get; set; }
}

/// <summary>Log de exibição / heartbeat.</summary>
public class TvDisplayLog : BaseEntity
{
    public Guid DisplayId { get; set; }
    public TvDisplay Display { get; set; } = null!;
    public string EventType { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
