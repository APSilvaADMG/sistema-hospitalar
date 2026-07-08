using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class PixChargeConfiguration : IEntityTypeConfiguration<PixCharge>
{
    public void Configure(EntityTypeBuilder<PixCharge> builder)
    {
        builder.Property(c => c.TxId).HasMaxLength(35).IsRequired();
        builder.Property(c => c.CopyPasteCode).HasMaxLength(512).IsRequired();
        builder.Property(c => c.Amount).HasPrecision(18, 2);
        builder.Property(c => c.PayerName).HasMaxLength(200);
        builder.Property(c => c.ProviderReference).HasMaxLength(100);

        builder.HasIndex(c => c.TxId).IsUnique();
        builder.HasIndex(c => new { c.FinancialAccountId, c.Status });

        builder.HasOne(c => c.FinancialAccount)
            .WithMany()
            .HasForeignKey(c => c.FinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Patient)
            .WithMany()
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
