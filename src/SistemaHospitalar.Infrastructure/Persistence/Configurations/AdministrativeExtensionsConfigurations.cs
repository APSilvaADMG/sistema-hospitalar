using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class TpaAdministratorConfiguration : IEntityTypeConfiguration<TpaAdministrator>
{
    public void Configure(EntityTypeBuilder<TpaAdministrator> builder)
    {
        builder.ToTable("tpa_administrators");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Cnpj).HasMaxLength(14);
        builder.Property(x => x.ContactName).HasMaxLength(120);
        builder.Property(x => x.ContactEmail).HasMaxLength(160);
        builder.Property(x => x.CommissionPercent).HasPrecision(10, 2);
        builder.Property(x => x.DiscountPercent).HasPrecision(10, 2);
    }
}

public class TpaClaimConfiguration : IEntityTypeConfiguration<TpaClaim>
{
    public void Configure(EntityTypeBuilder<TpaClaim> builder)
    {
        builder.ToTable("tpa_claims");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GrossAmount).HasPrecision(18, 2);
        builder.Property(x => x.CommissionAmount).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.NetAmount).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(600);
        builder.HasOne(x => x.TpaAdministrator).WithMany(x => x.Claims).HasForeignKey(x => x.TpaAdministratorId);
        builder.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId);
        builder.HasOne(x => x.HealthInsurance).WithMany().HasForeignKey(x => x.HealthInsuranceId);
        builder.HasOne(x => x.FinancialAccount).WithMany().HasForeignKey(x => x.FinancialAccountId);
        builder.HasIndex(x => new { x.TpaAdministratorId, x.ServiceDate });
    }
}

public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.ToTable("payroll_runs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalGross).HasPrecision(18, 2);
        builder.Property(x => x.TotalDiscounts).HasPrecision(18, 2);
        builder.Property(x => x.TotalNet).HasPrecision(18, 2);
        builder.Property(x => x.TotalFgtsEmployer).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(600);
        builder.HasOne(x => x.ConsolidatedFinancialAccount).WithMany().HasForeignKey(x => x.ConsolidatedFinancialAccountId);
        builder.HasIndex(x => new { x.Year, x.Month }).IsUnique();
    }
}

public class PayrollItemConfiguration : IEntityTypeConfiguration<PayrollItem>
{
    public void Configure(EntityTypeBuilder<PayrollItem> builder)
    {
        builder.ToTable("payroll_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BaseSalary).HasPrecision(18, 2);
        builder.Property(x => x.OvertimeAmount).HasPrecision(18, 2);
        builder.Property(x => x.BenefitsAmount).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.GrossAmount).HasPrecision(18, 2);
        builder.Property(x => x.NetAmount).HasPrecision(18, 2);
        builder.Property(x => x.FgtsEmployerAmount).HasPrecision(18, 2);
        builder.HasOne(x => x.PayrollRun).WithMany(x => x.Items).HasForeignKey(x => x.PayrollRunId);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId);
        builder.HasOne(x => x.FinancialAccount).WithMany().HasForeignKey(x => x.FinancialAccountId);
        builder.HasIndex(x => new { x.PayrollRunId, x.EmployeeId }).IsUnique();
    }
}

public class PayrollItemLineConfiguration : IEntityTypeConfiguration<PayrollItemLine>
{
    public void Configure(EntityTypeBuilder<PayrollItemLine> builder)
    {
        builder.ToTable("payroll_item_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasOne(x => x.PayrollItem).WithMany(x => x.Lines).HasForeignKey(x => x.PayrollItemId);
        builder.HasIndex(x => x.PayrollItemId);
    }
}

public class PharmacyBillingEntryConfiguration : IEntityTypeConfiguration<PharmacyBillingEntry>
{
    public void Configure(EntityTypeBuilder<PharmacyBillingEntry> builder)
    {
        builder.ToTable("pharmacy_billing_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasOne(x => x.Dispensing).WithMany().HasForeignKey(x => x.DispensingId);
        builder.HasOne(x => x.HealthInsurance).WithMany().HasForeignKey(x => x.HealthInsuranceId);
        builder.HasOne(x => x.FinancialAccount).WithMany().HasForeignKey(x => x.FinancialAccountId);
    }
}

public class BirthRegistrationConfiguration : IEntityTypeConfiguration<BirthRegistration>
{
    public void Configure(EntityTypeBuilder<BirthRegistration> builder)
    {
        builder.ToTable("birth_registrations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BabyName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.WeightKg).HasPrecision(8, 3);
        builder.Property(x => x.HeightCm).HasPrecision(8, 2);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasOne(x => x.MotherPatient).WithMany().HasForeignKey(x => x.MotherPatientId);
        builder.HasIndex(x => x.BirthAt);
    }
}

