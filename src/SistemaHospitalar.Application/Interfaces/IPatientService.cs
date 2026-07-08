using SistemaHospitalar.Application.Common;
using SistemaHospitalar.Application.DTOs.Patients;

namespace SistemaHospitalar.Application.Interfaces;

public interface IPatientService
{
    Task<PagedResult<PatientDto>> SearchAsync(
        string? search,
        int page,
        int pageSize,
        bool? isActive = true,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientDto>> QuickSearchAsync(string? search, int take = 10, CancellationToken cancellationToken = default);
    Task<PatientDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CreatePatientResult> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default);
    Task<PatientDetailDto?> UpdateAsync(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken = default);
    Task<CpfAvailabilityResult> CheckCpfAvailabilityAsync(
        string cpf,
        Guid? excludePatientId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PotentialDuplicatePatientDto>> FindPotentialDuplicatesAsync(
        PatientDuplicateCheckRequest request,
        CancellationToken cancellationToken = default);
}
