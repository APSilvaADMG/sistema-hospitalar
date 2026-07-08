using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class OfficialVersionConfiguration : IEntityTypeConfiguration<OfficialVersion>
{
    public void Configure(EntityTypeBuilder<OfficialVersion> builder)
    {
        builder.ToTable("tb_versao_oficial");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.VersionLabel).HasMaxLength(64).IsRequired();
        builder.Property(v => v.RemoteVersionLabel).HasMaxLength(64);
        builder.Property(v => v.InstalledFileHash).HasMaxLength(128);
        builder.Property(v => v.RemoteFileHash).HasMaxLength(128);
        builder.Property(v => v.SourceUrl).HasMaxLength(500);
        builder.Property(v => v.Notes).HasMaxLength(2000);
        builder.HasIndex(v => v.SourceType).IsUnique();
    }
}

public class IntegrationLogConfiguration : IEntityTypeConfiguration<IntegrationLog>
{
    public void Configure(EntityTypeBuilder<IntegrationLog> builder)
    {
        builder.ToTable("tb_log_integracao");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Action).HasMaxLength(64).IsRequired();
        builder.Property(l => l.Message).HasMaxLength(2000).IsRequired();
        builder.Property(l => l.TriggeredBy).HasMaxLength(128);
        builder.HasIndex(l => l.CreatedAt);
        builder.HasIndex(l => l.SourceType);
    }
}
