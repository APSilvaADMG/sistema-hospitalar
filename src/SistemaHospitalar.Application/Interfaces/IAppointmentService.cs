using SistemaHospitalar.Application.DTOs.Appointments;

namespace SistemaHospitalar.Application.Interfaces;

public interface IAppointmentService
{
    Task<IReadOnlyList<AppointmentDto>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<AppointmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CreateAppointmentResultDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<AppointmentDto?> UpdateAsync(Guid id, UpdateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<AppointmentDto?> UpdateStatusAsync(Guid id, UpdateAppointmentStatusRequest request, CancellationToken cancellationToken = default);
}
