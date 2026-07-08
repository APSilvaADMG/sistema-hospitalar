using SistemaHospitalar.Application.DTOs.Auth;
using SistemaHospitalar.Application.DTOs.Security;

namespace SistemaHospitalar.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<LoginResponse?> VerifyMfaLoginAsync(MfaLoginVerifyRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MfaSetupResponse> SetupMfaAsync(Guid userId, CancellationToken cancellationToken = default);
    Task EnableMfaAsync(Guid userId, MfaVerifyRequest request, CancellationToken cancellationToken = default);
    Task DisableMfaAsync(Guid userId, MfaVerifyRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<bool> VerifyUserPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);
}
