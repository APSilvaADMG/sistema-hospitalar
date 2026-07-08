using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Services;
using SistemaHospitalar.Infrastructure.Time;
using Xunit;

namespace SistemaHospitalar.Tests;

public class BiServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly BiService _service;
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _specialtyId = Guid.NewGuid();
    private readonly Guid _professionalId = Guid.NewGuid();

    public BiServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"bi-service-{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
        _service = new BiService(_db);

        _db.Specialties.Add(new Specialty { Id = _specialtyId, Name = "Clínica", IsActive = true });
        _db.Professionals.Add(new Professional
        {
            Id = _professionalId,
            FullName = "Dr. BI",
            SpecialtyId = _specialtyId,
            IsActive = true,
        });
        _db.Patients.Add(new Patient
        {
            Id = _patientId,
            FullName = "Paciente BI",
            Cpf = "11122233344",
            BirthDate = new DateOnly(1985, 5, 10),
            IsActive = true,
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetDashboardAsync_CountsActivePatients()
    {
        _db.Patients.Add(new Patient
        {
            FullName = "Inativo",
            Cpf = "99988877766",
            BirthDate = new DateOnly(1990, 1, 1),
            IsActive = false,
        });
        await _db.SaveChangesAsync();

        var dashboard = await _service.GetDashboardAsync(CancellationToken.None);

        Assert.Equal(1, dashboard.TotalPatients);
    }

    [Fact]
    public async Task GetDashboardAsync_AppointmentsToday_UsesBrazilDayBoundary()
    {
        var todayBrazil = HospitalTime.TodayInBrazil;
        var (startUtc, endUtc) = HospitalTime.BrazilDayRangeUtc(todayBrazil);

        _db.Appointments.AddRange(
            CreateAppointment(startUtc.AddHours(2), AppointmentStatus.Scheduled),
            CreateAppointment(endUtc.AddHours(1), AppointmentStatus.Scheduled),
            CreateAppointment(startUtc.AddHours(2), AppointmentStatus.Cancelled));

        await _db.SaveChangesAsync();

        var dashboard = await _service.GetDashboardAsync(CancellationToken.None);

        Assert.Equal(1, dashboard.AppointmentsToday);
    }

    [Fact]
    public async Task GetDashboardAsync_RevenueGrowth_ComputesMonthOverMonth()
    {
        var monthStartBrazil = new DateOnly(HospitalTime.TodayInBrazil.Year, HospitalTime.TodayInBrazil.Month, 1);
        var lastMonthStartBrazil = monthStartBrazil.AddMonths(-1);
        var (startOfMonth, _) = HospitalTime.BrazilDayRangeUtc(monthStartBrazil);
        var (startOfLastMonth, _) = HospitalTime.BrazilDayRangeUtc(lastMonthStartBrazil);

        var receivableThisMonth = CreateReceivable("Receita mês atual");
        var receivableLastMonth = CreateReceivable("Receita mês anterior");

        _db.FinancialAccounts.AddRange(receivableThisMonth, receivableLastMonth);
        await _db.SaveChangesAsync();

        _db.FinancialPayments.AddRange(
            new FinancialPayment
            {
                FinancialAccountId = receivableThisMonth.Id,
                Amount = 1500m,
                Method = PaymentMethod.Pix,
                PaidAt = startOfMonth.AddDays(2),
                IsActive = true,
            },
            new FinancialPayment
            {
                FinancialAccountId = receivableLastMonth.Id,
                Amount = 1000m,
                Method = PaymentMethod.Pix,
                PaidAt = startOfLastMonth.AddDays(5),
                IsActive = true,
            });
        await _db.SaveChangesAsync();

        var dashboard = await _service.GetDashboardAsync(CancellationToken.None);

        Assert.Equal(1500m, dashboard.RevenueThisMonth);
        Assert.Equal(1000m, dashboard.RevenueLastMonth);
        Assert.Equal(50m, dashboard.RevenueGrowthPercent);
    }

    [Fact]
    public async Task GetDashboardAsync_OccupancyRate_ReflectsOccupiedBeds()
    {
        var wardId = Guid.NewGuid();
        _db.Wards.Add(new Ward { Id = wardId, Name = "Enfermaria A", IsActive = true });
        _db.Beds.AddRange(
            new Bed { WardId = wardId, BedNumber = "101", Status = BedStatus.Occupied, IsActive = true },
            new Bed { WardId = wardId, BedNumber = "102", Status = BedStatus.Available, IsActive = true },
            new Bed { WardId = wardId, BedNumber = "103", Status = BedStatus.Occupied, IsActive = true },
            new Bed { WardId = wardId, BedNumber = "104", Status = BedStatus.Maintenance, IsActive = true });
        await _db.SaveChangesAsync();

        var dashboard = await _service.GetDashboardAsync(CancellationToken.None);

        Assert.Equal(4, dashboard.TotalBeds);
        Assert.Equal(2, dashboard.OccupiedBeds);
        Assert.Equal(50m, dashboard.BedOccupancyRate);
    }

    [Fact]
    public async Task GetDashboardAsync_MonthlyAppointmentsSeries_HasSixMonths()
    {
        var dashboard = await _service.GetDashboardAsync(CancellationToken.None);

        Assert.Equal(6, dashboard.MonthlyAppointments.Count);
        Assert.All(dashboard.MonthlyAppointments, m => Assert.Matches(@"\d{2}/\d{4}", m.Label));
    }

    [Fact]
    public async Task GetDashboardAsync_RevenueGrowth_WhenLastMonthZeroAndCurrentPositive_Returns100()
    {
        var monthStartBrazil = new DateOnly(HospitalTime.TodayInBrazil.Year, HospitalTime.TodayInBrazil.Month, 1);
        var (startOfMonth, _) = HospitalTime.BrazilDayRangeUtc(monthStartBrazil);
        var receivable = CreateReceivable("Somente mês atual");

        _db.FinancialAccounts.Add(receivable);
        await _db.SaveChangesAsync();

        _db.FinancialPayments.Add(new FinancialPayment
        {
            FinancialAccountId = receivable.Id,
            Amount = 500m,
            Method = PaymentMethod.Cash,
            PaidAt = startOfMonth.AddDays(1),
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var dashboard = await _service.GetDashboardAsync(CancellationToken.None);

        Assert.Equal(100m, dashboard.RevenueGrowthPercent);
    }

    private Appointment CreateAppointment(DateTime scheduledAt, AppointmentStatus status) =>
        new()
        {
            PatientId = _patientId,
            ProfessionalId = _professionalId,
            ScheduledAt = scheduledAt,
            DurationMinutes = 30,
            Status = status,
            IsActive = true,
        };

    private static FinancialAccount CreateReceivable(string description) => new()
    {
        Direction = FinancialAccountDirection.Receivable,
        Description = description,
        Amount = 2000m,
        Status = FinancialAccountStatus.Open,
        Category = FinancialAccountCategory.Consultation,
        IsActive = true,
    };

    public void Dispose() => _db.Dispose();
}
