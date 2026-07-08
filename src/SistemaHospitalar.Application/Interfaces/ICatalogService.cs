using SistemaHospitalar.Application.DTOs.Catalog;

namespace SistemaHospitalar.Application.Interfaces;

public interface ICatalogService
{
    Task<IReadOnlyList<HealthInsuranceDto>> GetHealthInsurancesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpecialtyDto>> GetSpecialtiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProfessionalDto>> GetProfessionalsAsync(CancellationToken cancellationToken = default);
}
