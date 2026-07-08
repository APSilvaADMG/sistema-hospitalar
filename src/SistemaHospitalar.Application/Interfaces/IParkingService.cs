using SistemaHospitalar.Application.DTOs.Parking;

namespace SistemaHospitalar.Application.Interfaces;

public interface IParkingService
{
    Task<IReadOnlyList<ParkingZoneDto>> GetZonesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ParkingSessionDto>> GetSessionsAsync(bool? activeOnly, CancellationToken cancellationToken = default);
    Task<ParkingSessionDto> CheckInAsync(CheckInParkingRequest request, CancellationToken cancellationToken = default);
    Task<ParkingSessionDto?> PaySessionAsync(PayParkingRequest request, CancellationToken cancellationToken = default);
    Task<ParkingSessionDto?> CheckOutAsync(CheckOutParkingRequest request, CancellationToken cancellationToken = default);
    Task<ParkingGateExitResultDto> ProcessGateExitAsync(ParkingGateExitRequest request, CancellationToken cancellationToken = default);
}
