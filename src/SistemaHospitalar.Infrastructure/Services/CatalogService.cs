using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Catalog;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class CatalogService(AppDbContext dbContext) : ICatalogService
{
    public async Task<IReadOnlyList<HealthInsuranceDto>> GetHealthInsurancesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.HealthInsurances
            .AsNoTracking()
            .Where(h => h.IsActive)
            .OrderBy(h => h.Name)
            .Select(h => new HealthInsuranceDto(
                h.Id,
                h.Name,
                h.AnsRegistration,
                h.Cnpj,
                h.LogoUrl,
                h.WebsiteUrl,
                h.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SpecialtyDto>> GetSpecialtiesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Specialties
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SpecialtyDto(s.Id, s.Name, s.CboCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProfessionalDto>> GetProfessionalsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Professionals
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.FullName)
            .Select(p => new ProfessionalDto(p.Id, p.FullName, p.Crm, p.SpecialtyId, p.Specialty.Name, p.PhotoData != null))
            .ToListAsync(cancellationToken);
    }
}
