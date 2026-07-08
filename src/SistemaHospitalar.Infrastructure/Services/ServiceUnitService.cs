using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Guides;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class ServiceUnitService(AppDbContext dbContext) : IServiceUnitService
{
    public async Task<IReadOnlyList<ServiceUnitDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.ServiceUnits.AsNoTracking()
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.IsDefault)
            .ThenBy(u => u.Name)
            .Select(Map())
            .ToListAsync(cancellationToken);

    public Task<ServiceUnitDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.ServiceUnits.AsNoTracking()
            .Where(u => u.Id == id && u.IsActive)
            .Select(Map())
            .FirstOrDefaultAsync(cancellationToken)!;

    public async Task<ServiceUnitDto> CreateAsync(
        CreateServiceUnitRequest request, CancellationToken cancellationToken = default)
    {
        if (request.IsDefault)
        {
            await ClearDefaultFlagsAsync(cancellationToken);
        }

        var unit = new ServiceUnit
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpperInvariant(),
            Cnes = request.Cnes?.Trim(),
            Address = request.Address?.Trim(),
            IsDefault = request.IsDefault,
        };

        dbContext.ServiceUnits.Add(unit);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(unit.Id, cancellationToken))!;
    }

    public async Task<ServiceUnitDto?> UpdateAsync(
        Guid id, UpdateServiceUnitRequest request, CancellationToken cancellationToken = default)
    {
        var unit = await dbContext.ServiceUnits.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (unit is null)
        {
            return null;
        }

        if (request.IsDefault && !unit.IsDefault)
        {
            await ClearDefaultFlagsAsync(cancellationToken);
        }

        unit.Name = request.Name.Trim();
        unit.Code = request.Code.Trim().ToUpperInvariant();
        unit.Cnes = request.Cnes?.Trim();
        unit.Address = request.Address?.Trim();
        unit.IsDefault = request.IsDefault;
        unit.IsActive = request.IsActive;
        unit.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task ClearDefaultFlagsAsync(CancellationToken cancellationToken)
    {
        await dbContext.ServiceUnits
            .Where(u => u.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsDefault, false), cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<ServiceUnit, ServiceUnitDto>> Map() =>
        u => new ServiceUnitDto(u.Id, u.Name, u.Code, u.Cnes, u.Address, u.IsDefault, u.IsActive);
}
