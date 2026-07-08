using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class PermissionDefinitionConfiguration : IEntityTypeConfiguration<PermissionDefinition>
{
    public void Configure(EntityTypeBuilder<PermissionDefinition> builder)
    {
        builder.ToTable("permission_definitions");
        builder.HasKey(p => p.Code);
        builder.Property(p => p.Code).HasMaxLength(80);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Module).HasMaxLength(80).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.Role, r.PermissionCode }).IsUnique();
        builder.HasOne(r => r.Permission).WithMany().HasForeignKey(r => r.PermissionCode);
    }
}

public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("login_attempts");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Email).HasMaxLength(200).IsRequired();
        builder.Property(l => l.FailureReason).HasMaxLength(500);
        builder.Property(l => l.IpAddress).HasMaxLength(50);
        builder.Property(l => l.UserAgent).HasMaxLength(500);
        builder.HasIndex(l => l.CreatedAt);
        builder.HasIndex(l => l.Email);
    }
}

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.IpAddress).HasMaxLength(50);
        builder.Property(s => s.UserAgent).HasMaxLength(500);
        builder.Property(s => s.DeviceId).HasMaxLength(100);
        builder.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
        builder.HasIndex(s => new { s.UserId, s.RevokedAt });
    }
}

public class ConsentTermConfiguration : IEntityTypeConfiguration<ConsentTerm>
{
    public void Configure(EntityTypeBuilder<ConsentTerm> builder)
    {
        builder.ToTable("consent_terms");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Version).HasMaxLength(40).IsRequired();
        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Content).IsRequired();
        builder.Property(c => c.PurposesJson).HasMaxLength(2000).IsRequired();
        builder.HasIndex(c => c.Version).IsUnique();
    }
}

public class PatientConsentConfiguration : IEntityTypeConfiguration<PatientConsent>
{
    public void Configure(EntityTypeBuilder<PatientConsent> builder)
    {
        builder.ToTable("patient_consents");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.PurposesJson).HasMaxLength(2000).IsRequired();
        builder.Property(c => c.IpAddress).HasMaxLength(50);
        builder.Property(c => c.Notes).HasMaxLength(1000);
        builder.Property(c => c.SignerName).HasMaxLength(120);
        builder.Property(c => c.SignatureHash).HasMaxLength(128);
        builder.HasOne(c => c.Patient).WithMany().HasForeignKey(c => c.PatientId);
        builder.HasOne(c => c.ConsentTerm).WithMany().HasForeignKey(c => c.ConsentTermId);
        builder.HasOne(c => c.RecordedByUser).WithMany().HasForeignKey(c => c.RecordedByUserId);
        builder.HasIndex(c => new { c.PatientId, c.RevokedAt });
    }
}

public class DataSubjectRequestConfiguration : IEntityTypeConfiguration<DataSubjectRequest>
{
    public void Configure(EntityTypeBuilder<DataSubjectRequest> builder)
    {
        builder.ToTable("data_subject_requests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Details).HasMaxLength(2000);
        builder.Property(r => r.ResponseNotes).HasMaxLength(2000);
        builder.Property(r => r.ExportFilePath).HasMaxLength(500);
        builder.HasOne(r => r.Patient).WithMany().HasForeignKey(r => r.PatientId);
        builder.HasOne(r => r.HandledByUser).WithMany().HasForeignKey(r => r.HandledByUserId);
        builder.HasIndex(r => r.Status);
    }
}

public class PrivacyIncidentConfiguration : IEntityTypeConfiguration<PrivacyIncident>
{
    public void Configure(EntityTypeBuilder<PrivacyIncident> builder)
    {
        builder.ToTable("privacy_incidents");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Title).HasMaxLength(200).IsRequired();
        builder.Property(i => i.IncidentType).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(4000).IsRequired();
        builder.Property(i => i.InvestigationNotes).HasMaxLength(4000);
        builder.Property(i => i.NotificationNotes).HasMaxLength(4000);
        builder.HasOne(i => i.ReportedByUser).WithMany().HasForeignKey(i => i.ReportedByUserId);
        builder.HasIndex(i => i.Status);
    }
}
