using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password, string? DeviceId = null);

public record LoginResponse(
    string? Token,
    bool RequiresMfa,
    string? MfaToken,
    Guid UserId,
    string FullName,
    string Email,
    UserRole Role,
    Guid? ProfessionalId,
    Guid? PatientId,
    IReadOnlyList<string> Permissions,
    bool MfaEnabled);

public record UserProfileDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    Guid? ProfessionalId,
    Guid? PatientId,
    IReadOnlyList<string> Permissions,
    bool MfaEnabled);

public record UserListDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    Guid? ProfessionalId,
    string? ProfessionalName,
    Guid? PatientId,
    string? PatientName);

public record UserDetailDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    Guid? ProfessionalId,
    string? ProfessionalName,
    Guid? PatientId,
    string? PatientName);

public record CreateUserRequest(
    string FullName,
    string Email,
    string Password,
    UserRole Role,
    Guid? ProfessionalId,
    Guid? PatientId);

public record UpdateUserRequest(
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    Guid? ProfessionalId,
    Guid? PatientId);

public record ResetUserPasswordRequest(string NewPassword);
