using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class PendingItemConfiguration : IEntityTypeConfiguration<PendingItem>
{
    public void Configure(EntityTypeBuilder<PendingItem> builder)
    {
        builder.ToTable("tb_pendencias");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Titulo).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Descricao).HasColumnType("text");
        builder.Property(p => p.Responsavel).HasMaxLength(200);
        builder.Property(p => p.Setor).HasMaxLength(200);
        builder.Property(p => p.LinkDestino).HasMaxLength(500);
        builder.Property(p => p.SourceEntityType).HasMaxLength(100);
        builder.HasOne(p => p.UsuarioResponsavel).WithMany().HasForeignKey(p => p.UsuarioResponsavelId);
        builder.HasIndex(p => new { p.UsuarioResponsavelId, p.Status, p.DataLimite });
        builder.HasIndex(p => new { p.UsuarioResponsavelId, p.SourceEntityType, p.SourceEntityId });
    }
}
