using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class PatientIdentityConfiguration : IEntityTypeConfiguration<PatientIdentity>
{
    public void Configure(EntityTypeBuilder<PatientIdentity> builder)
    {
        builder.ToTable("patient_identities");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Code).HasMaxLength(32).IsRequired();
        builder.HasIndex(i => i.Code).IsUnique();
        builder.Property(i => i.LabelContext).HasMaxLength(500);
        builder.HasOne(i => i.Patient).WithMany().HasForeignKey(i => i.PatientId);
        builder.HasOne(i => i.Hospitalization).WithMany().HasForeignKey(i => i.HospitalizationId);
        builder.HasIndex(i => new { i.PatientId, i.IdentityType, i.IsActive });
    }
}
