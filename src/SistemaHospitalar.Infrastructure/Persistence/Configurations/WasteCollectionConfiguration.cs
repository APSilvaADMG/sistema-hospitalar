using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class WasteCollectionConfiguration : IEntityTypeConfiguration<WasteCollection>
{
    public void Configure(EntityTypeBuilder<WasteCollection> builder)
    {
        builder.HasIndex(w => w.Code).IsUnique();
        builder.HasIndex(w => w.CollectedAt);
        builder.HasIndex(w => w.WasteType);
        builder.Property(w => w.Code).HasMaxLength(32);
        builder.Property(w => w.SectorName).HasMaxLength(120);
        builder.Property(w => w.ContainerCode).HasMaxLength(64);
        builder.Property(w => w.CollectedBy).HasMaxLength(120);
        builder.Property(w => w.ManifestNumber).HasMaxLength(64);
        builder.Property(w => w.QuantityKg).HasPrecision(10, 3);
    }
}
