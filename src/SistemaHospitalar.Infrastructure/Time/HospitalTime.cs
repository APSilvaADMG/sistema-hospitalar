namespace SistemaHospitalar.Infrastructure.Time;

/// <summary>
/// Horário operacional do hospital (Brasil — America/Sao_Paulo).
/// </summary>
public static class HospitalTime
{
    public static TimeZoneInfo BrazilTimeZone { get; } = ResolveBrazilTimeZone();

    public static DateOnly TodayInBrazil =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrazilTimeZone));

    public static DateTime BrazilLocalToUtc(DateTime localDateTime)
    {
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, BrazilTimeZone);
    }

    public static (DateTime StartUtc, DateTime EndUtc) BrazilDayRangeUtc(DateOnly localDate)
    {
        var startLocal = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var endLocal = localDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return (BrazilLocalToUtc(startLocal), BrazilLocalToUtc(endLocal));
    }

    private static TimeZoneInfo ResolveBrazilTimeZone()
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById("America/Sao_Paulo", out var iana))
        {
            return iana;
        }

        return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
    }
}
