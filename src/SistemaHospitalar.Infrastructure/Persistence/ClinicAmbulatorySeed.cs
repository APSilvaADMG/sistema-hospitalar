using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Dados ambulatoriais no estilo OnDoctor: Sala 1, Cliente Exemplo e agenda do dia.
/// </summary>
public static class ClinicAmbulatorySeed
{
    private const string DemoPatientCpf = "39053344705";
    private const string DemoMarker = "seed-ondoctor-demo";

    public static async Task EnsureAsync(
        AppDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Appointments.AnyAsync(a => a.Notes == DemoMarker, cancellationToken))
        {
            return;
        }

        var specialty = await dbContext.Specialties
            .FirstOrDefaultAsync(s => s.Name == "Clínica Geral", cancellationToken);
        if (specialty is null)
        {
            return;
        }

        var professional = await dbContext.Professionals
            .FirstOrDefaultAsync(p => p.Email == "anderson.pereira@hospital.local", cancellationToken);

        if (professional is null)
        {
            professional = new Professional
            {
                FullName = "Anderson Pereira Silva",
                Crm = "198765-SP",
                Email = "anderson.pereira@hospital.local",
                SpecialtyId = specialty.Id,
            };
            dbContext.Professionals.Add(professional);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var admin = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "admin@hospital.local", cancellationToken);
        if (admin is not null)
        {
            admin.FullName = professional.FullName;
            admin.ProfessionalId = professional.Id;
            admin.UpdatedAt = DateTime.UtcNow;
        }

        var patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Cpf == DemoPatientCpf, cancellationToken);

        if (patient is null)
        {
            patient = new Patient
            {
                FullName = "Cliente Exemplo",
                Cpf = DemoPatientCpf,
                BirthDate = new DateOnly(1990, 3, 15),
                Gender = Gender.Male,
                Email = "cliente.exemplo@email.com",
                Phone = "11999990000",
                MedicalRecord = new MedicalRecord { RecordNumber = "PEP-ONDOCTOR-0001" },
            };
            dbContext.Patients.Add(patient);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var room = await dbContext.ConsultingRooms
            .FirstOrDefaultAsync(r => r.Name == "Sala 1", cancellationToken);

        if (room is null)
        {
            room = new ConsultingRoom
            {
                Name = "Sala 1",
                Floor = "1",
                Building = "Ambulatório",
                SpecialtyId = specialty.Id,
                Status = ConsultingRoomStatus.Available,
            };
            dbContext.ConsultingRooms.Add(room);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var todayDow = BrazilToday().DayOfWeek;
        var hasSchedule = await dbContext.ConsultingRoomSchedules.AnyAsync(
            s => s.ProfessionalId == professional.Id && s.ConsultingRoomId == room.Id && s.DayOfWeek == todayDow,
            cancellationToken);

        if (!hasSchedule)
        {
            dbContext.ConsultingRoomSchedules.Add(new ConsultingRoomSchedule
            {
                ConsultingRoomId = room.Id,
                ProfessionalId = professional.Id,
                DayOfWeek = todayDow,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(18, 0),
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var today = BrazilToday();
        var morning = BrazilLocalToUtc(today.AddHours(10));
        var afternoon = BrazilLocalToUtc(today.AddHours(15));

        dbContext.Appointments.AddRange(
            new Appointment
            {
                PatientId = patient.Id,
                ProfessionalId = professional.Id,
                ScheduledAt = morning,
                DurationMinutes = 60,
                Status = AppointmentStatus.Scheduled,
                Reason = "Consulta — Particular",
                Room = room.Name,
                Notes = DemoMarker,
            },
            new Appointment
            {
                PatientId = patient.Id,
                ProfessionalId = professional.Id,
                ScheduledAt = afternoon,
                DurationMinutes = 30,
                Status = AppointmentStatus.Scheduled,
                Reason = "Avaliação técnica: perícia inicial ou final",
                Room = room.Name,
                Notes = DemoMarker,
            });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Agenda demo OnDoctor aplicada (Sala 1, Cliente Exemplo, 2 consultas hoje).");
    }

    private static DateTime BrazilToday()
    {
        var tz = ResolveBrazilTimeZone();
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
    }

    private static DateTime BrazilLocalToUtc(DateTime localDateTime)
    {
        var tz = ResolveBrazilTimeZone();
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, tz);
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
