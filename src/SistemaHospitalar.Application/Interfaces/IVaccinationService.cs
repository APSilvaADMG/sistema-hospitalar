using SistemaHospitalar.Application.DTOs.ClinicalOperations;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IVaccinationService
{
    Task<IReadOnlyList<VaccineCatalogDto>> ListCatalogAsync(
        VaccineScheduleType? scheduleType = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientVaccinationDto>> ListByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<PatientVaccinationDto> CreateAsync(
        CreatePatientVaccinationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EpidemicDiseaseCatalogDto>> ListEpidemicDiseasesAsync(
        string? search = null,
        CancellationToken cancellationToken = default);
}
