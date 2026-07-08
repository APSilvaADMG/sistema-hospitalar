using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Auth;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Infrastructure.Services;

public class UserService(AppDbContext dbContext) : IUserService
{
    public async Task<IReadOnlyList<UserListDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .Select(u => new UserListDto(
                u.Id,
                u.FullName,
                u.Email,
                u.Role,
                u.IsActive,
                u.CreatedAt,
                u.ProfessionalId,
                u.Professional != null ? u.Professional.FullName : null,
                u.PatientId,
                u.Patient != null ? u.Patient.FullName : null))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDetailDto(
                u.Id,
                u.FullName,
                u.Email,
                u.Role,
                u.IsActive,
                u.CreatedAt,
                u.ProfessionalId,
                u.Professional != null ? u.Professional.FullName : null,
                u.PatientId,
                u.Patient != null ? u.Patient.FullName : null))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var password = request.Password?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidOperationException("Informe o nome completo.");
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Informe o e-mail.");
        PasswordPolicy.ValidateOrThrow(password);
        if (!Enum.IsDefined(typeof(UserRole), request.Role))
            throw new InvalidOperationException("Perfil de acesso inválido.");

        if (await dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken))
            throw new InvalidOperationException("Já existe um usuário com este e-mail.");

        await ValidateLinksAsync(request.Role, request.ProfessionalId, request.PatientId, cancellationToken);

        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = request.Role,
            ProfessionalId = request.ProfessionalId,
            PatientId = request.PatientId,
            IsActive = true,
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(user.Id, cancellationToken))!;
    }

    public async Task<UserDetailDto?> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return null;

        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidOperationException("Informe o nome completo.");
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Informe o e-mail.");
        if (!Enum.IsDefined(typeof(UserRole), request.Role))
            throw new InvalidOperationException("Perfil de acesso inválido.");

        if (await dbContext.Users.AnyAsync(u => u.Id != id && u.Email == email, cancellationToken))
            throw new InvalidOperationException("Já existe um usuário com este e-mail.");

        if (id == currentUserId && !request.IsActive)
            throw new InvalidOperationException("Você não pode desativar o seu próprio usuário.");

        if (user.Role == UserRole.Admin && request.Role != UserRole.Admin && user.IsActive)
            await EnsureAnotherActiveAdminExistsAsync(id, cancellationToken);

        if (user.Role == UserRole.Admin && user.IsActive && !request.IsActive)
            await EnsureAnotherActiveAdminExistsAsync(id, cancellationToken);

        await ValidateLinksAsync(request.Role, request.ProfessionalId, request.PatientId, cancellationToken);

        user.FullName = fullName;
        user.Email = email;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.ProfessionalId = request.ProfessionalId;
        user.PatientId = request.PatientId;
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task ResetPasswordAsync(
        Guid id,
        ResetUserPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        var password = request.NewPassword?.Trim() ?? string.Empty;
        PasswordPolicy.ValidateOrThrow(password);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAnotherActiveAdminExistsAsync(Guid excludeUserId, CancellationToken cancellationToken)
    {
        var hasOtherAdmin = await dbContext.Users.AnyAsync(
            u => u.Id != excludeUserId && u.IsActive && u.Role == UserRole.Admin,
            cancellationToken);

        if (!hasOtherAdmin)
            throw new InvalidOperationException("Deve existir ao menos um administrador ativo no sistema.");
    }

    private async Task ValidateLinksAsync(
        UserRole role,
        Guid? professionalId,
        Guid? patientId,
        CancellationToken cancellationToken)
    {
        if (professionalId.HasValue)
        {
            var exists = await dbContext.Professionals.AnyAsync(
                p => p.Id == professionalId.Value && p.IsActive,
                cancellationToken);
            if (!exists)
                throw new InvalidOperationException("Profissional vinculado não encontrado.");
        }

        if (patientId.HasValue)
        {
            var exists = await dbContext.Patients.AnyAsync(
                p => p.Id == patientId.Value && p.IsActive,
                cancellationToken);
            if (!exists)
                throw new InvalidOperationException("Paciente vinculado não encontrado.");
        }

        if (role == UserRole.Doctor && !professionalId.HasValue)
            throw new InvalidOperationException("Usuários com perfil Médico devem ser vinculados a um profissional.");

        if (role == UserRole.Patient && !patientId.HasValue)
            throw new InvalidOperationException("Usuários com perfil Paciente devem ser vinculados a um paciente.");
    }
}
