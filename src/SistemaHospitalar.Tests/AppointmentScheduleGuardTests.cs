using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Services;
using Xunit;

namespace SistemaHospitalar.Tests;

public class AppointmentScheduleGuardTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Guid _professionalId;
    private readonly Guid _patientId;
    private static readonly DateTime SlotStart = new(2026, 7, 6, 14, 0, 0, DateTimeKind.Utc);

    public AppointmentScheduleGuardTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"appointment-guard-{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);

        var specialtyId = Guid.NewGuid();
        _professionalId = Guid.NewGuid();
        _patientId = Guid.NewGuid();

        _db.Specialties.Add(new Specialty { Id = specialtyId, Name = "Clínica Geral", IsActive = true });
        _db.Professionals.Add(new Professional
        {
            Id = _professionalId,
            FullName = "Dr. Teste",
            SpecialtyId = specialtyId,
            IsActive = true,
        });
        _db.Patients.Add(new Patient
        {
            Id = _patientId,
            FullName = "Paciente Teste",
            Cpf = "00000000000",
            BirthDate = new DateOnly(1990, 1, 1),
            IsActive = true,
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task HasProfessionalConflictAsync_SameProfessionalSameSlot_ReturnsTrue()
    {
        _db.Appointments.Add(new Appointment
        {
            PatientId = _patientId,
            ProfessionalId = _professionalId,
            ScheduledAt = SlotStart,
            DurationMinutes = 30,
            Status = AppointmentStatus.Scheduled,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var conflict = await AppointmentScheduleGuard.HasProfessionalConflictAsync(
            _db,
            _professionalId,
            SlotStart,
            30,
            excludeAppointmentId: null,
            CancellationToken.None);

        Assert.True(conflict);
    }

    [Fact]
    public async Task HasProfessionalConflictAsync_CancelledExisting_DoesNotBlock()
    {
        _db.Appointments.Add(new Appointment
        {
            PatientId = _patientId,
            ProfessionalId = _professionalId,
            ScheduledAt = SlotStart,
            DurationMinutes = 30,
            Status = AppointmentStatus.Cancelled,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var conflict = await AppointmentScheduleGuard.HasProfessionalConflictAsync(
            _db,
            _professionalId,
            SlotStart,
            30,
            excludeAppointmentId: null,
            CancellationToken.None);

        Assert.False(conflict);
    }

    [Fact]
    public async Task HasRoomConflictAsync_SameRoomSameSlot_ReturnsTrue()
    {
        const string room = "Consultório 1";
        _db.Appointments.Add(new Appointment
        {
            PatientId = _patientId,
            ProfessionalId = _professionalId,
            ScheduledAt = SlotStart,
            DurationMinutes = 30,
            Status = AppointmentStatus.Confirmed,
            Room = room,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var conflict = await AppointmentScheduleGuard.HasRoomConflictAsync(
            _db,
            room,
            SlotStart,
            30,
            excludeAppointmentId: null,
            CancellationToken.None);

        Assert.True(conflict);
    }

    [Fact]
    public async Task HasProfessionalConflictAsync_ExcludeSelf_AllowsReschedule()
    {
        var existingId = Guid.NewGuid();
        _db.Appointments.Add(new Appointment
        {
            Id = existingId,
            PatientId = _patientId,
            ProfessionalId = _professionalId,
            ScheduledAt = SlotStart,
            DurationMinutes = 30,
            Status = AppointmentStatus.Scheduled,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var conflict = await AppointmentScheduleGuard.HasProfessionalConflictAsync(
            _db,
            _professionalId,
            SlotStart,
            30,
            excludeAppointmentId: existingId,
            CancellationToken.None);

        Assert.False(conflict);
    }

    public void Dispose() => _db.Dispose();
}
