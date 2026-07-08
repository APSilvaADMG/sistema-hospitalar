using SistemaHospitalar.Application.DTOs.Patients;

namespace SistemaHospitalar.Application.Interfaces;

public interface IPatientTimelineService
{
    Task<PatientTimelineDto?> GetTimelineAsync(Guid patientId, CancellationToken cancellationToken = default);
}
