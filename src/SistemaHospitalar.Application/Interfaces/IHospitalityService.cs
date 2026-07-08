using SistemaHospitalar.Application.DTOs.Hospitality;

namespace SistemaHospitalar.Application.Interfaces;

public interface IHospitalityService
{
    Task<IReadOnlyList<HospitalityRoomDto>> GetRoomsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HospitalityBookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default);
    Task<HospitalityBookingDto> CreateBookingAsync(CreateHospitalityBookingRequest request, CancellationToken cancellationToken = default);
    Task<HospitalityBookingDto?> CheckInAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<HospitalityBookingDto?> CheckOutAsync(Guid bookingId, CancellationToken cancellationToken = default);
}
