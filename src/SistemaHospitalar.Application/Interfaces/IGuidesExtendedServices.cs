using SistemaHospitalar.Application.DTOs.Guides;

namespace SistemaHospitalar.Application.Interfaces;

public interface IServiceUnitService
{
    Task<IReadOnlyList<ServiceUnitDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServiceUnitDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceUnitDto> CreateAsync(CreateServiceUnitRequest request, CancellationToken cancellationToken = default);
    Task<ServiceUnitDto?> UpdateAsync(Guid id, UpdateServiceUnitRequest request, CancellationToken cancellationToken = default);
}

public interface ISusGuideService
{
    Task<IReadOnlyList<SusGuideDto>> SearchAsync(SusGuideFilterDto filter, CancellationToken cancellationToken = default);
    Task<int> CountAsync(SusGuideFilterDto filter, CancellationToken cancellationToken = default);
    Task<SusGuideDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SusGuideDto> CreateAsync(CreateSusGuideRequest request, CancellationToken cancellationToken = default);
    Task<SusGuideDto?> UpdateAsync(Guid id, UpdateSusGuideRequest request, CancellationToken cancellationToken = default);
    Task<SusGuideDto?> CancelAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SusGuideDto?> SubmitAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SusGuideDto?> AuthorizeAsync(Guid id, string? authorizationNumber, CancellationToken cancellationToken = default);
    Task<SusGuideDto?> DuplicateAsync(Guid id, CancellationToken cancellationToken = default);
}
