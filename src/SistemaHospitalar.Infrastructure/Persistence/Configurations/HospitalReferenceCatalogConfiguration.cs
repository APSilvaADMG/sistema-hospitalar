using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class HospitalReferenceCatalogConfiguration : IEntityTypeConfiguration<HospitalReferenceCatalogItem>
{
    public void Configure(EntityTypeBuilder<HospitalReferenceCatalogItem> builder)
    {
        builder.ToTable("hospital_reference_catalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ParentGroup).HasMaxLength(120);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.MetadataJson).HasMaxLength(2000);
        builder.Property(x => x.ContentRevision).HasDefaultValue(1);
        builder.HasIndex(x => new { x.CatalogType, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.CatalogType, x.ParentGroup, x.DisplayOrder });
    }
}
