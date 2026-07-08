using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public bool MfaEnabled { get; set; }
    public string? MfaSecretEncrypted { get; set; }

    public Guid? ProfessionalId { get; set; }
    public Professional? Professional { get; set; }

    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
}
