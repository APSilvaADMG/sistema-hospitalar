using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class PermissionService(AppDbContext dbContext) : IPermissionService
{
    public async Task<IReadOnlyList<string>> GetPermissionsForRoleAsync(
        UserRole role, CancellationToken cancellationToken = default)
    {
        return await dbContext.RolePermissions
            .AsNoTracking()
            .Where(r => r.Role == role)
            .Select(r => r.PermissionCode)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetPermissionsForUserAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => (UserRole?)u.Role)
            .FirstOrDefaultAsync(cancellationToken);

        return role is null ? [] : await GetPermissionsForRoleAsync(role.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionDefinitionDto>> GetAllDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PermissionDefinitions
            .AsNoTracking()
            .OrderBy(p => p.Module).ThenBy(p => p.Name)
            .Select(p => new PermissionDefinitionDto(p.Code, p.Name, p.Module, p.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RolePermissionDto>> GetRoleMatrixAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.RolePermissions
            .AsNoTracking()
            .OrderBy(r => r.Role).ThenBy(r => r.PermissionCode)
            .Select(r => new RolePermissionDto(r.Role, r.PermissionCode))
            .ToListAsync(cancellationToken);
    }
}
