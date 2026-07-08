using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder.ToTable("wards");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).HasMaxLength(150).IsRequired();
        builder.Property(w => w.Code).HasMaxLength(20);
        builder.Property(w => w.Floor).HasMaxLength(20);
        builder.Property(w => w.Description).HasMaxLength(500);
        builder.Property(w => w.CoverageModality).IsRequired();
        builder.Property(w => w.Category).IsRequired();
        builder.HasIndex(w => w.Code).IsUnique().HasFilter("\"Code\" IS NOT NULL");
    }
}

public class BedConfiguration : IEntityTypeConfiguration<Bed>
{
    public void Configure(EntityTypeBuilder<Bed> builder)
    {
        builder.ToTable("beds");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BedNumber).HasMaxLength(20).IsRequired();
        builder.Property(b => b.StatusReason).HasMaxLength(500);
        builder.HasOne(b => b.Ward).WithMany(w => w.Beds).HasForeignKey(b => b.WardId);
        builder.HasIndex(b => new { b.WardId, b.BedNumber }).IsUnique();
    }
}

public class HospitalizationConfiguration : IEntityTypeConfiguration<Hospitalization>
{
    public void Configure(EntityTypeBuilder<Hospitalization> builder)
    {
        builder.ToTable("hospitalizations");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Reason).HasMaxLength(500).IsRequired();
        builder.Property(h => h.Diagnosis).HasMaxLength(300);
        builder.Property(h => h.Notes).HasMaxLength(2000);
        builder.Property(h => h.AihNumber).HasMaxLength(20);
        builder.Property(h => h.SusCompetence).HasMaxLength(6);
        builder.Property(h => h.PrimaryCid10Code).HasMaxLength(10);
        builder.Property(h => h.SecondaryCid10Code).HasMaxLength(10);
        builder.Property(h => h.PrimarySigtapProcedureCode).HasMaxLength(20);
        builder.Property(h => h.SecondarySigtapProcedureCode).HasMaxLength(20);
        builder.Property(h => h.CnesCode).HasMaxLength(7);
        builder.Property(h => h.SusAuthorizationNumber).HasMaxLength(30);
        builder.HasOne(h => h.Patient).WithMany().HasForeignKey(h => h.PatientId);
        builder.HasOne(h => h.Bed).WithMany(b => b.Hospitalizations).HasForeignKey(h => h.BedId);
        builder.HasOne(h => h.Professional).WithMany().HasForeignKey(h => h.ProfessionalId);
        builder.HasOne(h => h.AiTriageLog).WithMany().HasForeignKey(h => h.AiTriageLogId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class HospitalizationSnippetConfiguration : IEntityTypeConfiguration<HospitalizationSnippet>
{
    public void Configure(EntityTypeBuilder<HospitalizationSnippet> builder)
    {
        builder.ToTable("hospitalization_snippets");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Text).HasMaxLength(500).IsRequired();
        builder.Property(s => s.NormalizedText).HasMaxLength(500).IsRequired();
        builder.HasIndex(s => new { s.Type, s.NormalizedText }).IsUnique();
        builder.HasIndex(s => new { s.Type, s.UsageCount });
    }
}

public class HospitalizationRequestConfiguration : IEntityTypeConfiguration<HospitalizationRequest>
{
    public void Configure(EntityTypeBuilder<HospitalizationRequest> builder)
    {
        builder.ToTable("hospitalization_requests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Reason).HasMaxLength(500).IsRequired();
        builder.Property(r => r.Diagnosis).HasMaxLength(300);
        builder.Property(r => r.Cid10Code).HasMaxLength(10);
        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.ReviewNotes).HasMaxLength(1000);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.RequestedAt);
        builder.HasIndex(r => new { r.PatientId, r.Status });
        builder.HasOne(r => r.Patient).WithMany().HasForeignKey(r => r.PatientId);
        builder.HasOne(r => r.RequestingProfessional).WithMany().HasForeignKey(r => r.RequestingProfessionalId);
        builder.HasOne(r => r.PreferredWard).WithMany().HasForeignKey(r => r.PreferredWardId);
        builder.HasOne(r => r.ReviewedByProfessional).WithMany().HasForeignKey(r => r.ReviewedByProfessionalId);
        builder.HasOne(r => r.Hospitalization).WithMany().HasForeignKey(r => r.HospitalizationId);
        builder.HasOne(r => r.AiTriageLog).WithMany().HasForeignKey(r => r.AiTriageLogId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class BedTransferConfiguration : IEntityTypeConfiguration<BedTransfer>
{
    public void Configure(EntityTypeBuilder<BedTransfer> builder)
    {
        builder.ToTable("bed_transfers");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Reason).HasMaxLength(500);
        builder.HasIndex(t => t.TransferredAt);
        builder.HasOne(t => t.Hospitalization).WithMany().HasForeignKey(t => t.HospitalizationId);
        builder.HasOne(t => t.FromBed).WithMany().HasForeignKey(t => t.FromBedId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.ToBed).WithMany().HasForeignKey(t => t.ToBedId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Professional).WithMany().HasForeignKey(t => t.ProfessionalId);
    }
}

public class OperatingRoomConfiguration : IEntityTypeConfiguration<OperatingRoom>
{
    public void Configure(EntityTypeBuilder<OperatingRoom> builder)
    {
        builder.ToTable("operating_rooms");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Location).HasMaxLength(100);
    }
}

public class SurgeryConfiguration : IEntityTypeConfiguration<Surgery>
{
    public void Configure(EntityTypeBuilder<Surgery> builder)
    {
        builder.ToTable("surgeries");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ProcedureName).HasMaxLength(300).IsRequired();
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.HasOne(s => s.Patient).WithMany().HasForeignKey(s => s.PatientId);
        builder.HasOne(s => s.OperatingRoom).WithMany(r => r.Surgeries).HasForeignKey(s => s.OperatingRoomId);
        builder.HasOne(s => s.Surgeon).WithMany().HasForeignKey(s => s.SurgeonId);
        builder.HasIndex(s => new { s.OperatingRoomId, s.ScheduledAt });
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Sku).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.Property(p => p.Unit).HasMaxLength(20).IsRequired();
        builder.Property(p => p.QuantityOnHand).HasPrecision(18, 3);
        builder.Property(p => p.MinimumStock).HasPrecision(18, 3);
        builder.Property(p => p.MaximumStock).HasPrecision(18, 3);
        builder.Property(p => p.ContentQuantity).HasPrecision(18, 3);
        builder.Property(p => p.AveragePurchasePrice).HasPrecision(18, 2);
        builder.Property(p => p.AverageSalePrice).HasPrecision(18, 2);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Presentation).HasMaxLength(120);
        builder.Property(p => p.Barcode).HasMaxLength(80);
        builder.Property(p => p.Category).HasMaxLength(120);
        builder.Property(p => p.Manufacturer).HasMaxLength(120);
        builder.Property(p => p.DefaultLocation).HasMaxLength(120);
        builder.Property(p => p.TussCode).HasMaxLength(40);
        builder.Property(p => p.EntryLocations).HasMaxLength(500);
        builder.Property(p => p.PhotoData).HasMaxLength(500_000);
    }
}

public class ProductKitConfiguration : IEntityTypeConfiguration<ProductKit>
{
    public void Configure(EntityTypeBuilder<ProductKit> builder)
    {
        builder.ToTable("product_kits");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Name).HasMaxLength(200).IsRequired();
        builder.Property(k => k.PriceTable).HasMaxLength(120);
    }
}

public class ProductKitItemConfiguration : IEntityTypeConfiguration<ProductKitItem>
{
    public void Configure(EntityTypeBuilder<ProductKitItem> builder)
    {
        builder.ToTable("product_kit_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.InsuranceCode).HasMaxLength(40);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.HasOne(i => i.ProductKit).WithMany(k => k.Items).HasForeignKey(i => i.ProductKitId);
        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId);
    }
}

public class StockRequisitionConfiguration : IEntityTypeConfiguration<StockRequisition>
{
    public void Configure(EntityTypeBuilder<StockRequisition> builder)
    {
        builder.ToTable("stock_requisitions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RequestNumber).HasMaxLength(30).IsRequired();
        builder.Property(r => r.OriginLocation).HasMaxLength(120);
        builder.Property(r => r.DestinationLocation).HasMaxLength(120);
        builder.Property(r => r.RequestedBy).HasMaxLength(120).IsRequired();
        builder.Property(r => r.RecipientName).HasMaxLength(120);
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.HasIndex(r => r.RequestNumber).IsUnique();
        builder.HasIndex(r => r.SequenceNumber);
    }
}

public class StockRequisitionItemConfiguration : IEntityTypeConfiguration<StockRequisitionItem>
{
    public void Configure(EntityTypeBuilder<StockRequisitionItem> builder)
    {
        builder.ToTable("stock_requisition_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.FulfilledQuantity).HasPrecision(18, 3);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.Notes).HasMaxLength(500);
        builder.HasOne(i => i.StockRequisition).WithMany(r => r.Items).HasForeignKey(i => i.StockRequisitionId);
        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Quantity).HasPrecision(18, 3);
        builder.Property(m => m.Reason).HasMaxLength(300).IsRequired();
        builder.Property(m => m.Reference).HasMaxLength(100);
        builder.Property(m => m.PatientOrSupplier).HasMaxLength(200);
        builder.Property(m => m.ResponsibleName).HasMaxLength(120);
        builder.Property(m => m.UserName).HasMaxLength(120);
        builder.Property(m => m.BatchNumber).HasMaxLength(80);
        builder.Property(m => m.IndividualCode).HasMaxLength(80);
        builder.Property(m => m.Location).HasMaxLength(120);
        builder.Property(m => m.InvoiceNumber).HasMaxLength(80);
        builder.Property(m => m.UnitPrice).HasPrecision(18, 2);
        builder.Property(m => m.Account).HasMaxLength(120);
        builder.HasOne(m => m.Product).WithMany(p => p.Movements).HasForeignKey(m => m.ProductId);
    }
}

public class ProductBillingRuleConfiguration : IEntityTypeConfiguration<ProductBillingRule>
{
    public void Configure(EntityTypeBuilder<ProductBillingRule> builder)
    {
        builder.ToTable("product_billing_rules");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.PriceTable).HasMaxLength(120).IsRequired();
        builder.Property(r => r.ReferenceTable).HasMaxLength(120);
        builder.Property(r => r.Code).HasMaxLength(80);
        builder.Property(r => r.PricePfb).HasPrecision(18, 2);
        builder.Property(r => r.Pmc).HasPrecision(18, 2);
        builder.Property(r => r.Edition).HasMaxLength(40);
        builder.HasOne(r => r.Product).WithMany(p => p.BillingRules).HasForeignKey(r => r.ProductId);
    }
}

public class InventoryLookupItemConfiguration : IEntityTypeConfiguration<InventoryLookupItem>
{
    public void Configure(EntityTypeBuilder<InventoryLookupItem> builder)
    {
        builder.ToTable("inventory_lookup_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(i => new { i.Type, i.Name }).IsUnique();
    }
}

public class MedicationInsuranceMappingConfiguration : IEntityTypeConfiguration<MedicationInsuranceMapping>
{
    public void Configure(EntityTypeBuilder<MedicationInsuranceMapping> builder)
    {
        builder.ToTable("medication_insurance_mappings");
        builder.HasKey(m => m.Id);
        builder.HasOne(m => m.PrescribedProduct).WithMany().HasForeignKey(m => m.PrescribedProductId);
        builder.HasOne(m => m.ReferenceProduct).WithMany().HasForeignKey(m => m.ReferenceProductId);
        builder.HasOne(m => m.HealthInsurance).WithMany().HasForeignKey(m => m.HealthInsuranceId);
        builder.HasIndex(m => new { m.PrescribedProductId, m.ReferenceProductId, m.HealthInsuranceId }).IsUnique();
    }
}

public class PharmacyDispensingConfiguration : IEntityTypeConfiguration<PharmacyDispensing>
{
    public void Configure(EntityTypeBuilder<PharmacyDispensing> builder)
    {
        builder.ToTable("pharmacy_dispensings");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Quantity).HasPrecision(18, 3);
        builder.Property(d => d.ReversedQuantity).HasPrecision(18, 3);
        builder.Property(d => d.Notes).HasMaxLength(500);
        builder.HasOne(d => d.Patient).WithMany().HasForeignKey(d => d.PatientId);
        builder.HasOne(d => d.Product).WithMany(p => p.Dispensings).HasForeignKey(d => d.ProductId);
        builder.HasOne(d => d.Professional).WithMany().HasForeignKey(d => d.ProfessionalId);
        builder.HasOne(d => d.Hospitalization).WithMany().HasForeignKey(d => d.HospitalizationId);
    }
}
