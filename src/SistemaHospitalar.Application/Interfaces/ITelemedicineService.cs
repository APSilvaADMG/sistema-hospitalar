using SistemaHospitalar.Application.DTOs.Telemedicine;

namespace SistemaHospitalar.Application.Interfaces;

public interface ITelemedicineService
{
    Task<IReadOnlyList<TelemedicineAppointmentDto>> GetAppointmentsAsync(CancellationToken cancellationToken = default);
    Task<TelemedicineAppointmentDto> CreateAppointmentAsync(CreateTelemedicineAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<TelemedicineAppointmentDto?> UpdateAppointmentStatusAsync(Guid id, UpdateTelemedicineStatusRequest request, CancellationToken cancellationToken = default);
}
