using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class ClinicalOperationsConfiguration :
    IEntityTypeConfiguration<FinancialAccountLineItem>,
    IEntityTypeConfiguration<FinancialCashSession>,
    IEntityTypeConfiguration<WardStockBalance>,
    IEntityTypeConfiguration<WardStockMovement>,
    IEntityTypeConfiguration<VaccineCatalog>,
    IEntityTypeConfiguration<PatientVaccination>,
    IEntityTypeConfiguration<EpidemicDiseaseCatalog>,
    IEntityTypeConfiguration<BedEvent>,
    IEntityTypeConfiguration<PharmacyDispensingReversal>,
    IEntityTypeConfiguration<AdministrationRouteCatalog>,
    IEntityTypeConfiguration<PatientReferenceCatalogItem>
{
    public void Configure(EntityTypeBuilder<FinancialAccountLineItem> builder)
    {
        builder.ToTable("financial_account_line_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(300);
        builder.Property(x => x.UnitAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.HasIndex(x => x.FinancialAccountId);
        builder.HasOne(x => x.FinancialAccount)
            .WithMany(f => f.LineItems)
            .HasForeignKey(x => x.FinancialAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<FinancialCashSession> builder)
    {
        builder.ToTable("financial_cash_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.OpeningBalance).HasPrecision(18, 2);
        builder.Property(x => x.ClosingBalance).HasPrecision(18, 2);
        builder.Property(x => x.ExpectedBalance).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.Status, x.OpenedAt });
    }

    public void Configure(EntityTypeBuilder<WardStockBalance> builder)
    {
        builder.ToTable("ward_stock_balances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QuantityOnHand).HasPrecision(18, 3);
        builder.Property(x => x.MinimumStock).HasPrecision(18, 3);
        builder.Property(x => x.Unit).HasMaxLength(20);
        builder.HasIndex(x => new { x.WardId, x.ProductId }).IsUnique();
        builder.HasOne(x => x.Ward).WithMany().HasForeignKey(x => x.WardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<WardStockMovement> builder)
    {
        builder.ToTable("ward_stock_movements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.Unit).HasMaxLength(20);
        builder.Property(x => x.Reference).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(300);
        builder.HasIndex(x => new { x.WardId, x.MovementDate });
        builder.HasOne(x => x.Ward).WithMany().HasForeignKey(x => x.WardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<VaccineCatalog> builder)
    {
        builder.ToTable("vaccine_catalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.ScheduleType, x.DisplayOrder });
    }

    public void Configure(EntityTypeBuilder<PatientVaccination> builder)
    {
        builder.ToTable("patient_vaccinations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BatchNumber).HasMaxLength(80);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => new { x.PatientId, x.AdministeredAt });
        builder.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.VaccineCatalog).WithMany().HasForeignKey(x => x.VaccineCatalogId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Professional).WithMany().HasForeignKey(x => x.ProfessionalId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<EpidemicDiseaseCatalog> builder)
    {
        builder.ToTable("epidemic_disease_catalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(250).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.DiseaseClass, x.DisplayOrder });
    }

    public void Configure(EntityTypeBuilder<BedEvent> builder)
    {
        builder.ToTable("bed_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.HasIndex(x => new { x.BedId, x.EndAt });
        builder.HasIndex(x => new { x.EventType, x.StartAt });
        builder.HasOne(x => x.Bed).WithMany().HasForeignKey(x => x.BedId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Hospitalization).WithMany().HasForeignKey(x => x.HospitalizationId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<PharmacyDispensingReversal> builder)
    {
        builder.ToTable("pharmacy_dispensing_reversals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.HasIndex(x => x.DispensingId);
        builder.HasOne(x => x.Dispensing).WithMany(d => d.Reversals).HasForeignKey(x => x.DispensingId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AdministrationRouteCatalog> builder)
    {
        builder.ToTable("administration_route_catalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Abbreviation).HasMaxLength(10);
        builder.HasIndex(x => x.Code).IsUnique();
    }

    public void Configure(EntityTypeBuilder<PatientReferenceCatalogItem> builder)
    {
        builder.ToTable("patient_reference_catalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.CatalogType, x.Code }).IsUnique();
    }
}
