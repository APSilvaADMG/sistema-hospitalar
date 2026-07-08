using SistemaHospitalar.Application.DTOs.Catalog;

namespace SistemaHospitalar.Application.Interfaces;

public interface IProfessionalService
{
    Task<IReadOnlyList<ProfessionalListDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProfessionalDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProfessionalDetailDto> CreateAsync(CreateProfessionalRequest request, CancellationToken cancellationToken = default);
    Task<ProfessionalDetailDto?> UpdateAsync(Guid id, UpdateProfessionalRequest request, CancellationToken cancellationToken = default);
}
