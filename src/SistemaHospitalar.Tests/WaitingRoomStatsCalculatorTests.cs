using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Services;
using Xunit;

namespace SistemaHospitalar.Tests;

public class WaitingRoomStatsCalculatorTests
{
    private static readonly DateTime Now = new(2026, 7, 7, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Compute_EmptyList_ReturnsZeros()
    {
        var stats = WaitingRoomStatsCalculator.Compute([], Now);

        Assert.Equal(0, stats.Waiting);
        Assert.Equal(0, stats.InCare);
        Assert.Equal(0, stats.CompletedToday);
        Assert.Equal(0, stats.AvgWaitMinutes);
        Assert.Empty(stats.ByRoom);
    }

    [Fact]
    public void Compute_MixedStatuses_CountsWaitingInCareAndCompleted()
    {
        var appts = new[]
        {
            Item(AppointmentStatus.Scheduled, Now.AddMinutes(-30), "Sala 1", "Ana"),
            Item(AppointmentStatus.Confirmed, Now.AddMinutes(-20), "Sala 1", "Bruno"),
            Item(AppointmentStatus.InProgress, Now.AddMinutes(-10), "Sala 2", "Carla"),
            Item(AppointmentStatus.Completed, Now.AddMinutes(-60), "Sala 3", "Diego"),
            Item(AppointmentStatus.Cancelled, Now, "Sala 4", "Elena"),
        };

        var stats = WaitingRoomStatsCalculator.Compute(appts, Now);

        Assert.Equal(2, stats.Waiting);
        Assert.Equal(1, stats.InCare);
        Assert.Equal(1, stats.CompletedToday);
    }

    [Fact]
    public void Compute_ByRoom_GroupsWaitingAndInCare()
    {
        var appts = new[]
        {
            Item(AppointmentStatus.Scheduled, Now.AddMinutes(-15), "Consultório 1", "Ana"),
            Item(AppointmentStatus.InProgress, Now.AddMinutes(-5), "Consultório 1", "Bruno"),
            Item(AppointmentStatus.Confirmed, Now.AddMinutes(-25), null, "Carla"),
        };

        var stats = WaitingRoomStatsCalculator.Compute(appts, Now);

        var room1 = Assert.Single(stats.ByRoom, r => r.Room == "Consultório 1");
        Assert.Equal(1, room1.Waiting);
        Assert.Equal(1, room1.InCare);

        var noRoom = Assert.Single(stats.ByRoom, r => r.Room == "Sem sala");
        Assert.Equal(1, noRoom.Waiting);
        Assert.Equal("Carla", noRoom.NextPatient);
    }

    [Fact]
    public void Compute_ConfirmedPatient_UsesElapsedWaitForAverage()
    {
        var appts = new[]
        {
            Item(AppointmentStatus.Confirmed, Now.AddMinutes(-40), "Sala 1", "Ana"),
        };

        var stats = WaitingRoomStatsCalculator.Compute(appts, Now);

        Assert.Equal(40, stats.AvgWaitMinutes);
    }

    [Fact]
    public void Compute_ScheduledOnly_UsesQueuePositionEstimate()
    {
        var appts = new[]
        {
            Item(AppointmentStatus.Scheduled, Now.AddMinutes(10), "Sala 1", "Ana"),
            Item(AppointmentStatus.Scheduled, Now.AddMinutes(20), "Sala 1", "Bruno"),
        };

        var stats = WaitingRoomStatsCalculator.Compute(appts, Now);

        Assert.Equal(18, stats.AvgWaitMinutes);
    }

    private static WaitingRoomStatsCalculator.AppointmentQueueItem Item(
        AppointmentStatus status,
        DateTime scheduledAt,
        string? room,
        string patient) =>
        new(status, scheduledAt, room, patient);
}
