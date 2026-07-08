using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class IntegrationMessageConfiguration : IEntityTypeConfiguration<IntegrationMessage>
{
    public void Configure(EntityTypeBuilder<IntegrationMessage> builder)
    {
        builder.ToTable("integration_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Source).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Destination).HasMaxLength(100);
        builder.Property(m => m.Payload).IsRequired();
        builder.Property(m => m.ErrorMessage).HasMaxLength(1000);
        builder.HasOne(m => m.Patient).WithMany().HasForeignKey(m => m.PatientId);
    }
}

public class Cid10CatalogConfiguration : IEntityTypeConfiguration<Cid10Catalog>
{
    public void Configure(EntityTypeBuilder<Cid10Catalog> builder)
    {
        builder.ToTable("cid10_catalog");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(10).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.Description).HasMaxLength(300).IsRequired();
        builder.Property(c => c.ParentCode).HasMaxLength(10);
        builder.Property(c => c.Category).HasMaxLength(100);
        builder.HasIndex(c => c.ParentCode);
        builder.Property(c => c.Keywords).HasMaxLength(500);
    }
}

public class AiTriageLogConfiguration : IEntityTypeConfiguration<AiTriageLog>
{
    public void Configure(EntityTypeBuilder<AiTriageLog> builder)
    {
        builder.ToTable("ai_triage_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Symptoms).HasMaxLength(2000).IsRequired();
        builder.Property(l => l.RecommendedSpecialty).HasMaxLength(100).IsRequired();
        builder.Property(l => l.SuggestedCid10).HasMaxLength(10);
        builder.Property(l => l.SuggestedCid10Description).HasMaxLength(300);
        builder.Property(l => l.Notes).HasMaxLength(1000);
        builder.HasOne(l => l.Patient).WithMany().HasForeignKey(l => l.PatientId);
    }
}
