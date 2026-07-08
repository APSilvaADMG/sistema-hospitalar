using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class LabExamCatalogSpecialtyConfiguration : IEntityTypeConfiguration<LabExamCatalogSpecialty>
{
    public void Configure(EntityTypeBuilder<LabExamCatalogSpecialty> builder)
    {
        builder.ToTable("lab_exam_catalog_specialties");
        builder.HasKey(x => new { x.LabExamCatalogId, x.SpecialtyId });
        builder.HasOne(x => x.LabExamCatalog).WithMany(e => e.SpecialtyLinks).HasForeignKey(x => x.LabExamCatalogId);
        builder.HasOne(x => x.Specialty).WithMany().HasForeignKey(x => x.SpecialtyId);
    }
}

public class ImagingProcedureCatalogConfiguration : IEntityTypeConfiguration<ImagingProcedureCatalog>
{
    public void Configure(EntityTypeBuilder<ImagingProcedureCatalog> builder)
    {
        builder.ToTable("imaging_procedure_catalogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TussCode).HasMaxLength(20);
        builder.Property(x => x.BodyPart).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}

public class ImagingProcedureSpecialtyConfiguration : IEntityTypeConfiguration<ImagingProcedureSpecialty>
{
    public void Configure(EntityTypeBuilder<ImagingProcedureSpecialty> builder)
    {
        builder.ToTable("imaging_procedure_specialties");
        builder.HasKey(x => new { x.ImagingProcedureCatalogId, x.SpecialtyId });
        builder.HasOne(x => x.ImagingProcedureCatalog).WithMany(p => p.SpecialtyLinks).HasForeignKey(x => x.ImagingProcedureCatalogId);
        builder.HasOne(x => x.Specialty).WithMany().HasForeignKey(x => x.SpecialtyId);
    }
}

public class MedicationCatalogConfiguration : IEntityTypeConfiguration<MedicationCatalog>
{
    public void Configure(EntityTypeBuilder<MedicationCatalog> builder)
    {
        builder.ToTable("medication_catalogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ActiveIngredient).HasMaxLength(500);
        builder.Property(x => x.PharmaceuticalForm).HasMaxLength(80);
        builder.Property(x => x.Strength).HasMaxLength(80);
        builder.Property(x => x.DefaultDosage).HasMaxLength(200);
        builder.Property(x => x.Route).HasMaxLength(80);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.PackageInsert).HasColumnType("text");
        builder.Property(x => x.ExternalBulaSlug).HasMaxLength(120);
        builder.HasIndex(x => x.ExternalBulaSlug).IsUnique();
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class MedicationCatalogSpecialtyConfiguration : IEntityTypeConfiguration<MedicationCatalogSpecialty>
{
    public void Configure(EntityTypeBuilder<MedicationCatalogSpecialty> builder)
    {
        builder.ToTable("medication_catalog_specialties");
        builder.HasKey(x => new { x.MedicationCatalogId, x.SpecialtyId });
        builder.HasOne(x => x.MedicationCatalog).WithMany(m => m.SpecialtyLinks).HasForeignKey(x => x.MedicationCatalogId);
        builder.HasOne(x => x.Specialty).WithMany().HasForeignKey(x => x.SpecialtyId);
    }
}
