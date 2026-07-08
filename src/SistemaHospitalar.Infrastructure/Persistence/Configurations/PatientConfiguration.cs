using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FullName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.SocialName).HasMaxLength(200);
        builder.Property(p => p.Cpf).HasMaxLength(512).IsRequired();
        builder.Property(p => p.CpfHash).HasMaxLength(64);
        builder.Property(p => p.CnsHash).HasMaxLength(64);
        builder.Property(p => p.Cns).HasMaxLength(512);
        builder.Property(p => p.Rg).HasMaxLength(512);
        builder.Property(p => p.Phone).HasMaxLength(512);
        builder.Property(p => p.MobilePhone).HasMaxLength(512);
        builder.Property(p => p.EmergencyContactPhone).HasMaxLength(512);
        builder.Property(p => p.AddressStreet).HasMaxLength(512);
        builder.Property(p => p.AddressNumber).HasMaxLength(512);
        builder.Property(p => p.AddressComplement).HasMaxLength(512);
        builder.Property(p => p.AddressNeighborhood).HasMaxLength(512);
        builder.Property(p => p.AddressZipCode).HasMaxLength(512);
        builder.Property(p => p.Email).HasMaxLength(512);
        builder.Property(p => p.AddressCity).HasMaxLength(100);
        builder.Property(p => p.AddressState).HasMaxLength(2);
        builder.Property(p => p.MotherName).HasMaxLength(200);
        builder.Property(p => p.EmergencyContactName).HasMaxLength(200);
        builder.Property(p => p.EmergencyContactRelationship).HasMaxLength(80);
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.PhotoData).HasColumnType("text");
        builder.Property(p => p.Nationality).HasMaxLength(80);
        builder.Property(p => p.BloodType).HasMaxLength(5);
        builder.Property(p => p.Occupation).HasMaxLength(120);
        builder.Property(p => p.MaritalStatus).HasMaxLength(30);
        builder.Property(p => p.BirthPlace).HasMaxLength(120);
        builder.Property(p => p.UsesResponsibleCpf).HasDefaultValue(false);
        builder.Property(p => p.LegalResponsibleName).HasMaxLength(200);
        builder.Property(p => p.LegalResponsibleRg).HasMaxLength(512);
        builder.Property(p => p.LegalAuthorizationDocumentReference).HasMaxLength(120);
        builder.HasIndex(p => p.CpfHash)
            .IsUnique()
            .HasFilter("\"UsesResponsibleCpf\" = false AND \"CpfHash\" IS NOT NULL");
        builder.HasIndex(p => p.CnsHash)
            .IsUnique()
            .HasFilter("\"CnsHash\" IS NOT NULL");
    }
}
