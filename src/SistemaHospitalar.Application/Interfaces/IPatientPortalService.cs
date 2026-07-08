using SistemaHospitalar.Application.DTOs.PatientPortal;

namespace SistemaHospitalar.Application.Interfaces;

public interface IPatientPortalService
{
    Task<PatientPortalDashboardDto?> GetDashboardAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<PatientMedicalRecordDto?> GetMedicalRecordAsync(Guid patientId, CancellationToken cancellationToken = default);
}
