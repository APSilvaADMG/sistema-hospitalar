using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.TvSignage;

public record TvLayoutZoneDto(
    string Id,
    TvWidgetType Widget,
    int X,
    int Y,
    int W,
    int H);

public record TvLayoutDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<TvLayoutZoneDto> Zones,
    bool IsSystem);

public record TvDisplayDto(
    Guid Id,
    string Name,
    string Slug,
    string? Sector,
    string? IpAddress,
    string? Resolution,
    TvDisplayOrientation Orientation,
    TvDisplayStatus Status,
    string PlayerToken,
    Guid? LayoutId,
    string? LayoutName,
    bool ShowPatientName,
    bool EnableSound,
    int CallDisplaySeconds,
    string? WeatherCity,
    DateTime? LastSeenAt,
    string PlayerUrl);

public record TvMediaDto(
    Guid Id,
    string Title,
    TvMediaType MediaType,
    string Url,
    string? MimeType,
    string? Sector,
    DateTime? StartsAt,
    DateTime? EndsAt,
    int Priority,
    int DurationSeconds);

public record TvNewsDto(
    Guid Id,
    string Title,
    string? Summary,
    string? ImageUrl,
    string? Sector,
    DateTime PublishedAt,
    DateTime? ExpiresAt);

public record TvAnnouncementDto(
    Guid Id,
    string Title,
    string Body,
    string? Sector,
    DateTime StartsAt,
    DateTime? EndsAt,
    int Priority);

public record TvQueueCallDto(
    Guid Id,
    string TicketNumber,
    string? PatientName,
    string Destination,
    string? Sector,
    DateTime CalledAt,
    int DisplaySeconds,
    bool ShowPatientName,
    bool IsActive);

public record TvWeatherDto(
    string City,
    decimal TemperatureC,
    string Condition,
    string? Icon,
    int HumidityPercent,
    DateTime UpdatedAt);

public record TvDashboardWidgetDto(
    int AttendancesToday,
    int EmergencyWaiting,
    double AverageEmergencyWaitMinutes,
    decimal BedOccupancyRate,
    int LabOrdersPending);

public record TvScheduleItemDto(
    string Name,
    string RoleOrSpecialty,
    string ShiftLabel,
    string? TimeLabel);

public record TvCampaignDto(
    Guid Id,
    string Name,
    string? Sector,
    DateTime StartsAt,
    DateTime? EndsAt,
    string? DailyStart,
    string? DailyEnd,
    string? DaysOfWeek,
    int Priority,
    IReadOnlyList<Guid> MediaIds);

public record TvPlayerStateDto(
    TvDisplayDto Display,
    TvLayoutDto Layout,
    IReadOnlyList<TvMediaDto> Media,
    IReadOnlyList<TvNewsDto> News,
    IReadOnlyList<TvAnnouncementDto> Announcements,
    IReadOnlyList<TvQueueCallDto> RecentCalls,
    TvQueueCallDto? ActiveCall,
    TvWeatherDto? Weather,
    TvDashboardWidgetDto? Dashboard,
    IReadOnlyList<TvScheduleItemDto> Schedule,
    string SpeechProvider,
    string? ActiveCallSpeechUrl,
    DateTime GeneratedAt);

public record TvMonitorSummaryDto(
    int TotalDisplays,
    int OnlineDisplays,
    int OfflineDisplays,
    int CallsToday,
    int ActiveMedia,
    IReadOnlyList<TvDisplayDto> Displays);

public record CreateTvDisplayRequest(
    string Name,
    string Slug,
    string? Sector,
    Guid? LayoutId,
    TvDisplayOrientation Orientation,
    string? Resolution,
    string? WeatherCity,
    bool ShowPatientName,
    bool EnableSound,
    int CallDisplaySeconds);

public record UpdateTvDisplayRequest(
    string Name,
    string? Sector,
    Guid? LayoutId,
    TvDisplayOrientation Orientation,
    string? Resolution,
    string? WeatherCity,
    bool ShowPatientName,
    bool EnableSound,
    int CallDisplaySeconds);

public record CreateTvLayoutRequest(string Name, string? Description, IReadOnlyList<TvLayoutZoneDto> Zones);

public record UpdateTvLayoutRequest(string Name, string? Description, IReadOnlyList<TvLayoutZoneDto> Zones);

public record CreateTvMediaRequest(
    string Title,
    TvMediaType MediaType,
    string? Sector,
    DateTime? StartsAt,
    DateTime? EndsAt,
    int Priority,
    int DurationSeconds);

public record CreateTvNewsRequest(
    string Title,
    string? Summary,
    string? ImageUrl,
    string? Sector,
    DateTime? ExpiresAt);

public record CreateTvAnnouncementRequest(
    string Title,
    string Body,
    string? Sector,
    DateTime? StartsAt,
    DateTime? EndsAt,
    int Priority);

public record CallTvQueueRequest(
    string TicketNumber,
    string? PatientName,
    string Destination,
    string? Sector,
    Guid? DisplayId,
    bool? ShowPatientName,
    Guid? KioskTicketId = null);

public record CallKioskTicketRequest(
    string Destination,
    Guid? DisplayId,
    bool? ShowPatientName);

public record CreateTvCampaignRequest(
    string Name,
    string? Sector,
    DateTime? StartsAt,
    DateTime? EndsAt,
    string? DailyStart,
    string? DailyEnd,
    string? DaysOfWeek,
    int Priority,
    IReadOnlyList<Guid> MediaIds);

public record UpdateTvCampaignRequest(
    string Name,
    string? Sector,
    DateTime? StartsAt,
    DateTime? EndsAt,
    string? DailyStart,
    string? DailyEnd,
    string? DaysOfWeek,
    int Priority,
    IReadOnlyList<Guid> MediaIds);

public record TvHeartbeatRequest(string? IpAddress, string? Resolution);
