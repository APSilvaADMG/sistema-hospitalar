using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Services;
using Xunit;

namespace SistemaHospitalar.Tests;

public class AppointmentSchedulingEngineTests
{
    private static readonly DateTime SlotStart = new(2026, 7, 6, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IntervalsOverlap_WhenSameSlot_ReturnsTrue()
    {
        var overlaps = AppointmentSchedulingEngine.IntervalsOverlap(
            SlotStart, 30, SlotStart, 30);

        Assert.True(overlaps);
    }

    [Fact]
    public void IntervalsOverlap_WhenAdjacent_ReturnsFalse()
    {
        var overlaps = AppointmentSchedulingEngine.IntervalsOverlap(
            SlotStart, 30, SlotStart.AddMinutes(30), 30);

        Assert.False(overlaps);
    }

    [Fact]
    public void IsBlocking_CancelledAppointment_ReturnsFalse()
    {
        Assert.False(AppointmentSchedulingEngine.IsBlocking(AppointmentStatus.Cancelled));
        Assert.False(AppointmentSchedulingEngine.IsBlocking(AppointmentStatus.NoShow));
        Assert.False(AppointmentSchedulingEngine.IsBlocking(AppointmentStatus.Scheduled, isActive: false));
    }

    [Fact]
    public void IsBlocking_ScheduledActive_ReturnsTrue()
    {
        Assert.True(AppointmentSchedulingEngine.IsBlocking(AppointmentStatus.Scheduled));
        Assert.True(AppointmentSchedulingEngine.IsBlocking(AppointmentStatus.Confirmed));
        Assert.True(AppointmentSchedulingEngine.IsBlocking(AppointmentStatus.InProgress));
    }

    [Fact]
    public void HasConflict_SameProfessionalOverlappingSlot_ReturnsTrue()
    {
        var existing = new[]
        {
            new AppointmentConflictCheck(SlotStart, 30, AppointmentStatus.Scheduled),
        };

        var conflict = AppointmentSchedulingEngine.HasConflict(
            existing,
            Guid.NewGuid(),
            SlotStart.AddMinutes(15),
            30);

        Assert.True(conflict);
    }

    [Fact]
    public void HasConflict_CancelledExisting_DoesNotBlock()
    {
        var existing = new[]
        {
            new AppointmentConflictCheck(SlotStart, 30, AppointmentStatus.Cancelled),
        };

        var conflict = AppointmentSchedulingEngine.HasConflict(
            existing,
            Guid.NewGuid(),
            SlotStart,
            30);

        Assert.False(conflict);
    }

    [Fact]
    public void IntervalsOverlap_WhenCandidateStartsBeforeExistingEnds_ReturnsTrue()
    {
        var overlaps = AppointmentSchedulingEngine.IntervalsOverlap(
            SlotStart, 60, SlotStart.AddMinutes(45), 30);

        Assert.True(overlaps);
    }

    [Fact]
    public void HasConflict_WhenCandidateEndsExactlyAtExistingStart_ReturnsFalse()
    {
        var existing = new[]
        {
            new AppointmentConflictCheck(SlotStart.AddMinutes(30), 30, AppointmentStatus.Scheduled),
        };

        var conflict = AppointmentSchedulingEngine.HasConflict(
            existing,
            Guid.NewGuid(),
            SlotStart,
            30);

        Assert.False(conflict);
    }

    [Fact]
    public void IsBlocking_CompletedAppointment_ReturnsTrue()
    {
        Assert.True(AppointmentSchedulingEngine.IsBlocking(AppointmentStatus.Completed));
    }

    [Theory]
    [InlineData(AppointmentKind.Consulta, 30)]
    [InlineData(AppointmentKind.Retorno, 15)]
    [InlineData(AppointmentKind.Exame, 45)]
    public void AppointmentDurationRules_ReturnExpectedMinutes(AppointmentKind kind, int expected)
    {
        Assert.Equal(expected, AppointmentDurationRules.GetMinutes(kind));
    }

    [Theory]
    [InlineData("2026-07-06", true)]
    [InlineData("2026-07-05", false)]
    [InlineData("2026-07-04", false)]
    public void SchedulingBusinessHours_IsWorkingDay_WeekdaysOnly(string isoDate, bool expected)
    {
        var hours = new SchedulingBusinessHours();
        var date = DateOnly.Parse(isoDate);

        Assert.Equal(expected, hours.IsWorkingDay(date));
    }
}
