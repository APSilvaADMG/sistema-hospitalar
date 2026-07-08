using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Hospitality;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class HospitalityService(AppDbContext dbContext) : IHospitalityService
{
    public async Task<IReadOnlyList<HospitalityRoomDto>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.HospitalityRooms
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.RoomNumber)
            .Select(r => new HospitalityRoomDto(r.Id, r.RoomNumber, r.Floor, r.Capacity, r.DailyRate, r.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HospitalityBookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.HospitalityBookings
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.CheckInDate)
            .Select(b => new HospitalityBookingDto(
                b.Id, b.HospitalityRoomId, b.HospitalityRoom.RoomNumber, b.GuestName,
                b.Patient != null ? b.Patient.FullName : null, b.Status,
                b.CheckInDate, b.CheckOutDate, b.HospitalityRoom.DailyRate, b.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<HospitalityBookingDto> CreateBookingAsync(
        CreateHospitalityBookingRequest request, CancellationToken cancellationToken = default)
    {
        var room = await dbContext.HospitalityRooms
            .FirstOrDefaultAsync(r => r.Id == request.RoomId && r.IsActive, cancellationToken);

        if (room is null || room.Status != HospitalityRoomStatus.Available)
        {
            throw new InvalidOperationException("Quarto indisponível.");
        }

        var booking = new HospitalityBooking
        {
            HospitalityRoomId = request.RoomId,
            PatientId = request.PatientId,
            GuestName = request.GuestName.Trim(),
            GuestDocument = request.GuestDocument?.Trim(),
            GuestPhone = request.GuestPhone?.Trim(),
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            Notes = request.Notes?.Trim()
        };

        dbContext.HospitalityBookings.Add(booking);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetBookingsAsync(cancellationToken)).First(b => b.Id == booking.Id);
    }

    public async Task<HospitalityBookingDto?> CheckInAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await dbContext.HospitalityBookings
            .Include(b => b.HospitalityRoom)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.IsActive, cancellationToken);

        if (booking is null || booking.Status != HospitalityBookingStatus.Reserved)
        {
            return null;
        }

        booking.Status = HospitalityBookingStatus.CheckedIn;
        booking.ActualCheckIn = DateTime.UtcNow;
        booking.HospitalityRoom.Status = HospitalityRoomStatus.Occupied;
        booking.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetBookingsAsync(cancellationToken)).FirstOrDefault(b => b.Id == bookingId);
    }

    public async Task<HospitalityBookingDto?> CheckOutAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await dbContext.HospitalityBookings
            .Include(b => b.HospitalityRoom)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.IsActive, cancellationToken);

        if (booking is null || booking.Status != HospitalityBookingStatus.CheckedIn)
        {
            return null;
        }

        booking.Status = HospitalityBookingStatus.CheckedOut;
        booking.ActualCheckOut = DateTime.UtcNow;
        booking.HospitalityRoom.Status = HospitalityRoomStatus.Cleaning;
        booking.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetBookingsAsync(cancellationToken)).FirstOrDefault(b => b.Id == bookingId);
    }
}
