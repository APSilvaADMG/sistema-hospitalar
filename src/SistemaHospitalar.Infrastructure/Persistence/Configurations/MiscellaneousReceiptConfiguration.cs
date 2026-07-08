using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class MiscellaneousReceiptConfiguration : IEntityTypeConfiguration<MiscellaneousReceipt>
{
    public void Configure(EntityTypeBuilder<MiscellaneousReceipt> builder)
    {
        builder.ToTable("miscellaneous_receipts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReceiptNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.PayerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ReceiverName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(120);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasIndex(x => x.ReceiptNumber).IsUnique();
        builder.HasIndex(x => x.ReceiptDate);
    }
}
