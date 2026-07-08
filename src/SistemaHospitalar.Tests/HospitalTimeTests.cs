using SistemaHospitalar.Infrastructure.Time;
using Xunit;

namespace SistemaHospitalar.Tests;

public class HospitalTimeTests
{
    [Fact]
    public void BrazilTimeZone_IsResolved()
    {
        Assert.NotNull(HospitalTime.BrazilTimeZone);
        Assert.Contains("America", HospitalTime.BrazilTimeZone.Id, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BrazilDayRangeUtc_CoversFullLocalDay()
    {
        var date = new DateOnly(2026, 7, 6);
        var (startUtc, endUtc) = HospitalTime.BrazilDayRangeUtc(date);

        Assert.True(endUtc > startUtc);
        var span = endUtc - startUtc;
        Assert.InRange(span.TotalHours, 23.5, 24.5);
    }

    [Fact]
    public void BrazilLocalToUtc_MidnightBrazil_StartsDayRange()
    {
        var date = new DateOnly(2026, 1, 15);
        var midnightLocal = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var utc = HospitalTime.BrazilLocalToUtc(midnightLocal);
        var (startUtc, _) = HospitalTime.BrazilDayRangeUtc(date);

        Assert.Equal(startUtc, utc);
    }

    [Fact]
    public void BrazilDayRangeUtc_ConsecutiveDays_DoNotOverlap()
    {
        var day1 = new DateOnly(2026, 3, 10);
        var day2 = day1.AddDays(1);

        var (_, endDay1) = HospitalTime.BrazilDayRangeUtc(day1);
        var (startDay2, _) = HospitalTime.BrazilDayRangeUtc(day2);

        Assert.Equal(endDay1, startDay2);
    }

    [Fact]
    public void TodayInBrazil_IsWithinReasonableWindow()
    {
        var today = HospitalTime.TodayInBrazil;
        var utcToday = DateOnly.FromDateTime(DateTime.UtcNow);
        var diff = Math.Abs(today.DayNumber - utcToday.DayNumber);

        Assert.True(diff <= 1, $"TodayInBrazil ({today}) diverged too far from UTC date ({utcToday}).");
    }
}
