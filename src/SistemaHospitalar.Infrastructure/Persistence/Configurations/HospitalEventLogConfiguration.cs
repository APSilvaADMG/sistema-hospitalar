using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class HospitalEventLogConfiguration : IEntityTypeConfiguration<HospitalEventLog>
{
    public void Configure(EntityTypeBuilder<HospitalEventLog> builder)
    {
        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId });
        builder.Property(e => e.EventType).HasMaxLength(120);
        builder.Property(e => e.RoutingKey).HasMaxLength(120);
        builder.Property(e => e.RelatedEntityType).HasMaxLength(80);
        builder.Property(e => e.PayloadJson).HasColumnType("jsonb");
    }
}
