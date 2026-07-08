using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class ProductLotConfiguration : IEntityTypeConfiguration<ProductLot>
{
    public void Configure(EntityTypeBuilder<ProductLot> builder)
    {
        builder.ToTable("estoque_lotes");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.BatchNumber).HasMaxLength(80).IsRequired();
        builder.Property(l => l.Manufacturer).HasMaxLength(120);
        builder.Property(l => l.QuantityOnHand).HasPrecision(18, 3);
        builder.Property(l => l.LocationName).HasMaxLength(120);
        builder.Property(l => l.UnitCost).HasPrecision(18, 2);
        builder.HasOne(l => l.Product).WithMany(p => p.Lots).HasForeignKey(l => l.ProductId);
        builder.HasIndex(l => new { l.ProductId, l.BatchNumber })
            .IsUnique()
            .HasFilter("\"IsActive\" = true");
    }
}

public class StockReceiptConfiguration : IEntityTypeConfiguration<StockReceipt>
{
    public void Configure(EntityTypeBuilder<StockReceipt> builder)
    {
        builder.ToTable("estoque_entradas");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.SupplierName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.SupplierCnpj).HasMaxLength(18);
        builder.Property(r => r.InvoiceNumber).HasMaxLength(80);
        builder.Property(r => r.InvoiceSeries).HasMaxLength(10);
        builder.Property(r => r.NfeAccessKey).HasMaxLength(44);
        builder.Property(r => r.TotalAmount).HasPrecision(18, 2);
        builder.Property(r => r.FreightAmount).HasPrecision(18, 2);
        builder.Property(r => r.DiscountAmount).HasPrecision(18, 2);
        builder.Property(r => r.PaymentCondition).HasMaxLength(60);
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.Property(r => r.ReceivedByUserName).HasMaxLength(120);
        builder.HasIndex(r => r.ReceivedAt);
        builder.HasIndex(r => r.InvoiceNumber);
        builder.HasIndex(r => r.NfeAccessKey);
    }
}

public class StockReceiptItemConfiguration : IEntityTypeConfiguration<StockReceiptItem>
{
    public void Configure(EntityTypeBuilder<StockReceiptItem> builder)
    {
        builder.ToTable("estoque_entrada_itens");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.BatchNumber).HasMaxLength(80).IsRequired();
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.LineTotal).HasPrecision(18, 2);
        builder.Property(i => i.Ncm).HasMaxLength(10);
        builder.Property(i => i.Cfop).HasMaxLength(10);
        builder.HasOne(i => i.StockReceipt).WithMany(r => r.Items).HasForeignKey(i => i.StockReceiptId);
        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId);
        builder.HasOne(i => i.ProductLot)
            .WithMany()
            .HasForeignKey(i => i.ProductLotId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class StockIssueConfiguration : IEntityTypeConfiguration<StockIssue>
{
    public void Configure(EntityTypeBuilder<StockIssue> builder)
    {
        builder.ToTable("estoque_saidas");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.SectorName).HasMaxLength(120).IsRequired();
        builder.Property(i => i.ResponsibleName).HasMaxLength(120).IsRequired();
        builder.Property(i => i.Notes).HasMaxLength(500);
        builder.HasOne(i => i.Patient).WithMany().HasForeignKey(i => i.PatientId).IsRequired(false);
        builder.HasOne(i => i.Hospitalization).WithMany().HasForeignKey(i => i.HospitalizationId).IsRequired(false);
        builder.HasIndex(i => i.CreatedAt);
    }
}

public class StockIssueItemConfiguration : IEntityTypeConfiguration<StockIssueItem>
{
    public void Configure(EntityTypeBuilder<StockIssueItem> builder)
    {
        builder.ToTable("estoque_saida_itens");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.HasOne(i => i.StockIssue).WithMany(s => s.Items).HasForeignKey(i => i.StockIssueId);
        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId);
        builder.HasOne(i => i.ProductLot).WithMany().HasForeignKey(i => i.ProductLotId).IsRequired(false);
    }
}
