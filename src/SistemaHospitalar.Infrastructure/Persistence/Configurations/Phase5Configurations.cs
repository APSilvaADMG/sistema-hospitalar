using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class EmergencyVisitConfiguration : IEntityTypeConfiguration<EmergencyVisit>
{
    public void Configure(EntityTypeBuilder<EmergencyVisit> builder)
    {
        builder.ToTable("emergency_visits");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.ChiefComplaint).HasMaxLength(500).IsRequired();
        builder.Property(v => v.Notes).HasMaxLength(1000);
        builder.HasOne(v => v.Patient).WithMany().HasForeignKey(v => v.PatientId);
        builder.HasOne(v => v.Professional).WithMany().HasForeignKey(v => v.ProfessionalId);
        builder.HasOne(v => v.AiTriageLog).WithMany().HasForeignKey(v => v.AiTriageLogId);
    }
}

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Cnpj).HasMaxLength(14);
        builder.Property(s => s.Email).HasMaxLength(200);
        builder.Property(s => s.Phone).HasMaxLength(20);
        builder.Property(s => s.ContactName).HasMaxLength(200);
    }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.RequestedBy).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Justification).HasMaxLength(500);
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.HasOne(o => o.Supplier).WithMany().HasForeignKey(o => o.SupplierId);
    }
}

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("purchase_order_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.HasOne(i => i.PurchaseOrder).WithMany(o => o.Items).HasForeignKey(i => i.PurchaseOrderId);
        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.UserEmail).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Details).HasMaxLength(2000).IsRequired();
        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.UserAgent).HasMaxLength(500);
        builder.Property(a => a.DeviceId).HasMaxLength(100);
        builder.Property(a => a.ActionCategory).HasMaxLength(50);
        builder.Property(a => a.BeforeSnapshot).HasMaxLength(4000);
        builder.Property(a => a.AfterSnapshot).HasMaxLength(4000);
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.EntityType);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.RelatedEntityType).HasMaxLength(100);
        builder.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId);
        builder.HasIndex(n => new { n.UserId, n.IsRead });
    }
}
