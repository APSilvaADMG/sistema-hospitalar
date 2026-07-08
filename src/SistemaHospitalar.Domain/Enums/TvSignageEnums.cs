namespace SistemaHospitalar.Domain.Enums;

public enum TvDisplayOrientation
{
    Horizontal = 1,
    Vertical = 2,
}

public enum TvDisplayStatus
{
    Online = 1,
    Offline = 2,
}

public enum TvMediaType
{
    Image = 1,
    Video = 2,
    Pdf = 3,
    Slideshow = 4,
}

public enum TvWidgetType
{
    MediaCarousel = 1,
    QueueCalls = 2,
    NewsTicker = 3,
    Weather = 4,
    Clock = 5,
    Dashboard = 6,
    Announcements = 7,
    Bulletin = 8,
    Schedule = 9,
}

public enum TvQueueCallMode
{
    TicketOnly = 1,
    NameAndDestination = 2,
}
