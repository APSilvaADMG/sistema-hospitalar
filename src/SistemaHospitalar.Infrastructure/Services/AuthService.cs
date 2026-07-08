using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.DTOs.Auth;
using SistemaHospitalar.Application.DTOs.Security;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Auth;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Infrastructure.Services;

public class AuthService(
    AppDbContext dbContext,
    JwtTokenGenerator tokenGenerator,
    IPermissionService permissionService,
    IFieldEncryptionService encryptionService,
    IAuditService auditService,
    IOptions<JwtSettings> jwtOptions) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is not null && user.LockoutEndUtc.HasValue && user.LockoutEndUtc > DateTime.UtcNow)
        {
            await RecordLoginAttempt(email, false, user.Id, ipAddress, userAgent, "Conta bloqueada temporariamente.", cancellationToken);
            throw new InvalidOperationException("Conta bloqueada temporariamente. Tente novamente mais tarde.");
        }

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            if (user is not null)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= PasswordPolicy.MaxFailedAttempts)
                {
                    user.LockoutEndUtc = DateTime.UtcNow.Add(PasswordPolicy.LockoutDuration);
                    user.FailedLoginAttempts = 0;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await RecordLoginAttempt(email, false, user?.Id, ipAddress, userAgent, "Credenciais inválidas.", cancellationToken);
            return null;
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        await RecordLoginAttempt(email, true, user.Id, ipAddress, userAgent, null, cancellationToken);

        var permissions = await permissionService.GetPermissionsForUserAsync(user.Id, cancellationToken);

        if (user.MfaEnabled)
        {
            var mfaToken = tokenGenerator.GenerateMfaChallengeToken(user);
            return BuildResponse(user, null, true, mfaToken, permissions);
        }

        var session = await CreateSessionAsync(user, ipAddress, userAgent, request.DeviceId, cancellationToken);
        var token = tokenGenerator.GenerateToken(user, permissions, session.Id);

        await auditService.LogAsync(new Application.DTOs.Audit.CreateAuditLogRequest(
            user.Id, user.Email, "Login", "auth", user.Id,
            "Login realizado com sucesso.", ipAddress, userAgent, request.DeviceId, "Login", true),
            cancellationToken);

        return BuildResponse(user, token, false, null, permissions);
    }

    public async Task<LoginResponse?> VerifyMfaLoginAsync(
        MfaLoginVerifyRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(request.MfaToken, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Value.Issuer,
            ValidAudience = jwtOptions.Value.Audience,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtOptions.Value.Key)),
        }, out _);

        if (principal.FindFirst("mfa_challenge")?.Value != "true")
        {
            throw new InvalidOperationException("Token MFA inválido.");
        }

        var userIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);
        if (user is null || !user.MfaEnabled || string.IsNullOrEmpty(user.MfaSecretEncrypted))
        {
            return null;
        }

        var secret = encryptionService.Decrypt(user.MfaSecretEncrypted);
        if (!TotpService.Verify(secret, request.Code))
        {
            await RecordLoginAttempt(user.Email, false, user.Id, ipAddress, userAgent, "Código MFA inválido.", cancellationToken);
            throw new InvalidOperationException("Código MFA inválido.");
        }

        var permissions = await permissionService.GetPermissionsForUserAsync(user.Id, cancellationToken);
        var session = await CreateSessionAsync(user, ipAddress, userAgent, null, cancellationToken);
        var token = tokenGenerator.GenerateToken(user, permissions, session.Id);

        await auditService.LogAsync(new Application.DTOs.Audit.CreateAuditLogRequest(
            user.Id, user.Email, "MfaLogin", "auth", user.Id,
            "Login com MFA concluído.", ipAddress, userAgent, null, "Login", true),
            cancellationToken);

        return BuildResponse(user, token, false, null, permissions);
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new { u.Id, u.FullName, u.Email, u.Role, u.ProfessionalId, u.PatientId, u.MfaEnabled })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var permissions = await permissionService.GetPermissionsForUserAsync(userId, cancellationToken);

        return new UserProfileDto(
            user.Id, user.FullName, user.Email, user.Role,
            user.ProfessionalId, user.PatientId, permissions, user.MfaEnabled);
    }

    public async Task<MfaSetupResponse> SetupMfaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        var secret = TotpService.GenerateSecret();
        user.MfaSecretEncrypted = encryptionService.Encrypt(secret);
        user.MfaEnabled = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MfaSetupResponse(secret, TotpService.BuildUri(secret, user.Email), secret);
    }

    public async Task EnableMfaAsync(Guid userId, MfaVerifyRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        if (string.IsNullOrEmpty(user.MfaSecretEncrypted))
        {
            throw new InvalidOperationException("Configure o MFA antes de ativar.");
        }

        var secret = encryptionService.Decrypt(user.MfaSecretEncrypted);
        if (!TotpService.Verify(secret, request.Code))
        {
            throw new InvalidOperationException("Código MFA inválido.");
        }

        user.MfaEnabled = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DisableMfaAsync(Guid userId, MfaVerifyRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        if (!user.MfaEnabled || string.IsNullOrEmpty(user.MfaSecretEncrypted))
        {
            user.MfaEnabled = false;
            user.MfaSecretEncrypted = null;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var secret = encryptionService.Decrypt(user.MfaSecretEncrypted);
        if (!TotpService.Verify(secret, request.Code))
        {
            throw new InvalidOperationException("Código MFA inválido.");
        }

        user.MfaEnabled = false;
        user.MfaSecretEncrypted = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task LogoutAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && s.RevokedAt == null, cancellationToken);

        if (session is not null)
        {
            session.RevokedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> VerifyUserPasswordAsync(
        Guid userId, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        return user is not null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    private async Task<UserSession> CreateSessionAsync(
        User user, string? ip, string? userAgent, string? deviceId, CancellationToken cancellationToken)
    {
        var session = new UserSession
        {
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddHours(jwtOptions.Value.ExpiresHours),
            IpAddress = ip,
            UserAgent = userAgent,
            DeviceId = deviceId,
        };

        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    private async Task RecordLoginAttempt(
        string email, bool success, Guid? userId, string? ip, string? userAgent,
        string? failureReason, CancellationToken cancellationToken)
    {
        dbContext.LoginAttempts.Add(new LoginAttempt
        {
            Email = email,
            Success = success,
            UserId = userId,
            IpAddress = ip,
            UserAgent = userAgent,
            FailureReason = failureReason,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static LoginResponse BuildResponse(
        User user, string? token, bool requiresMfa, string? mfaToken, IReadOnlyList<string> permissions)
        => new(token, requiresMfa, mfaToken, user.Id, user.FullName, user.Email,
            user.Role, user.ProfessionalId, user.PatientId, permissions, user.MfaEnabled);
}
