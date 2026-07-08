using SistemaHospitalar.Application.DTOs.MedicalRecords;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Services;

public class DigitalRecordService(
    IMedicalRecordService medicalRecordService,
    IHospitalizationService hospitalizationService,
    ITissBillingService tissBillingService) : IDigitalRecordService
{
    public async Task<DigitalRecordSummaryDto?> GetByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var record = await medicalRecordService.GetByPatientIdAsync(patientId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var hospitalizations = await hospitalizationService.GetByPatientAsync(patientId, cancellationToken);
        var active = hospitalizations.FirstOrDefault(h => h.Status == HospitalizationStatus.Active);
        var guides = await tissBillingService.GetGuidesAsync(null, patientId, null, cancellationToken);

        return new DigitalRecordSummaryDto(record, active, hospitalizations, guides);
    }
}
