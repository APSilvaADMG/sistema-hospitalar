using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public static class ConnectSchedulingHelper
{
    private static readonly TimeOnly DayStart = new(8, 0);
    private static readonly TimeOnly DayEnd = new(17, 0);
    private const int SlotMinutes = 30;

    public static async Task<IReadOnlyList<AvailableSlotDto>> GetAvailableSlotsAsync(
        AppDbContext db,
        Guid? specialtyId,
        Guid? professionalId,
        DateOnly fromDate,
        int days = 7,
        int maxSlots = 12,
        CancellationToken cancellationToken = default)
    {
        var professionalsQuery = db.Professionals.AsNoTracking().Where(p => p.IsActive);
        if (specialtyId.HasValue)
        {
            professionalsQuery = professionalsQuery.Where(p => p.SpecialtyId == specialtyId.Value);
        }

        if (professionalId.HasValue)
        {
            professionalsQuery = professionalsQuery.Where(p => p.Id == professionalId.Value);
        }

        var professionals = await professionalsQuery
            .Select(p => new { p.Id, p.FullName, SpecialtyName = p.Specialty.Name })
            .ToListAsync(cancellationToken);

        if (professionals.Count == 0)
        {
            return [];
        }

        var profIds = professionals.Select(p => p.Id).ToList();
        var rangeStart = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var rangeEnd = fromDate.AddDays(days).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var appointments = await db.Appointments.AsNoTracking()
            .Where(a => profIds.Contains(a.ProfessionalId)
                && a.IsActive
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow
                && a.ScheduledAt >= rangeStart
                && a.ScheduledAt < rangeEnd)
            .Select(a => new { a.ProfessionalId, a.ScheduledAt, a.DurationMinutes })
            .ToListAsync(cancellationToken);

        var slots = new List<AvailableSlotDto>();

        for (var day = 0; day < days && slots.Count < maxSlots; day++)
        {
            var date = fromDate.AddDays(day);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            foreach (var prof in professionals)
            {
                for (var time = DayStart; time < DayEnd && slots.Count < maxSlots; time = time.AddMinutes(SlotMinutes))
                {
                    var slotStart = date.ToDateTime(time, DateTimeKind.Utc);
                    if (slotStart <= DateTime.UtcNow)
                    {
                        continue;
                    }

                    var slotEnd = slotStart.AddMinutes(SlotMinutes);
                    var conflict = appointments.Any(a =>
                        a.ProfessionalId == prof.Id
                        && a.ScheduledAt < slotEnd
                        && a.ScheduledAt.AddMinutes(a.DurationMinutes) > slotStart);

                    if (!conflict)
                    {
                        slots.Add(new AvailableSlotDto(slotStart, prof.Id, prof.FullName, prof.SpecialtyName));
                    }
                }
            }
        }

        return slots.OrderBy(s => s.ScheduledAt).Take(maxSlots).ToList();
    }

    public static string FormatSlot(AvailableSlotDto slot)
        => $"{slot.ScheduledAt.ToLocalTime():dd/MM HH:mm} — Dr(a). {slot.ProfessionalName}";

    public static string GenerateProtocol(Guid appointmentId)
        => $"AGD-{DateTime.UtcNow:yyyyMMdd}-{appointmentId.ToString()[..8].ToUpperInvariant()}";
}
