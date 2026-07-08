using SistemaHospitalar.Application.DTOs.MedicalRecords;

namespace SistemaHospitalar.Application.Interfaces;

public interface IDigitalRecordService
{
    Task<DigitalRecordSummaryDto?> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
}
