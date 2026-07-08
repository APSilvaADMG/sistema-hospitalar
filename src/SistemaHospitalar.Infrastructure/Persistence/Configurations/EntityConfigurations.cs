using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class HealthInsuranceConfiguration : IEntityTypeConfiguration<HealthInsurance>
{
    public void Configure(EntityTypeBuilder<HealthInsurance> builder)
    {
        builder.ToTable("health_insurances");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Name).HasMaxLength(200).IsRequired();
        builder.Property(h => h.AnsRegistration).HasMaxLength(20);
        builder.Property(h => h.Cnpj).HasMaxLength(14);
        builder.Property(h => h.Phone).HasMaxLength(20);
        builder.Property(h => h.Email).HasMaxLength(200);
        builder.Property(h => h.TissVersion).HasMaxLength(20);
        builder.Property(h => h.OperatorCode).HasMaxLength(30);
        builder.Property(h => h.LogoUrl).HasMaxLength(500);
        builder.Property(h => h.WebsiteUrl).HasMaxLength(300);
        builder.Property(h => h.IntegrationSecret).HasMaxLength(200);
        builder.Property(h => h.UseMockIntegration).HasDefaultValue(true);
        builder.Property(h => h.BusinessRules).HasMaxLength(2000);
        builder.Property(h => h.AuthorizationDeadlineDays);
        builder.Property(h => h.RequiresOnlineAuthorization).HasDefaultValue(false);
    }
}

public class PatientInsuranceConfiguration : IEntityTypeConfiguration<PatientInsurance>
{
    public void Configure(EntityTypeBuilder<PatientInsurance> builder)
    {
        builder.ToTable("patient_insurances");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.CardNumber).HasMaxLength(50).IsRequired();
        builder.Property(p => p.PlanName).HasMaxLength(100);
        builder.Property(p => p.CardHolderName).HasMaxLength(200);
        builder.Property(p => p.ProductCode).HasMaxLength(30);
        builder.Property(p => p.CnsNumber).HasMaxLength(20);
        builder.Property(p => p.AccommodationType).HasMaxLength(50);
        builder.HasOne(p => p.Patient).WithMany(p => p.Insurances).HasForeignKey(p => p.PatientId);
        builder.HasOne(p => p.HealthInsurance).WithMany(h => h.PatientInsurances).HasForeignKey(p => p.HealthInsuranceId);
    }
}

public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("specialties");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
        builder.Property(s => s.CboCode).HasMaxLength(10);
    }
}

public class ProfessionalConfiguration : IEntityTypeConfiguration<Professional>
{
    public void Configure(EntityTypeBuilder<Professional> builder)
    {
        builder.ToTable("professionals");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FullName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Crm).HasMaxLength(20);
        builder.Property(p => p.Cpf).HasMaxLength(11);
        builder.Property(p => p.Email).HasMaxLength(200);
        builder.Property(p => p.Phone).HasMaxLength(20);
        builder.Property(p => p.SocialName).HasMaxLength(200);
        builder.Property(p => p.CouncilUf).HasMaxLength(2);
        builder.Property(p => p.Rg).HasMaxLength(20);
        builder.Property(p => p.MobilePhone).HasMaxLength(20);
        builder.Property(p => p.AddressStreet).HasMaxLength(200);
        builder.Property(p => p.AddressNumber).HasMaxLength(20);
        builder.Property(p => p.AddressComplement).HasMaxLength(100);
        builder.Property(p => p.AddressNeighborhood).HasMaxLength(100);
        builder.Property(p => p.AddressCity).HasMaxLength(100);
        builder.Property(p => p.AddressState).HasMaxLength(2);
        builder.Property(p => p.AddressZipCode).HasMaxLength(8);
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.PhotoData).HasColumnType("text");
        builder.HasOne(p => p.Specialty).WithMany(s => s.Professionals).HasForeignKey(p => p.SpecialtyId);
    }
}

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.Property(a => a.Notes).HasMaxLength(2000);
        builder.Property(a => a.Room).HasMaxLength(50);
        builder.HasOne(a => a.Patient).WithMany(p => p.Appointments).HasForeignKey(a => a.PatientId);
        builder.HasOne(a => a.Professional).WithMany(p => p.Appointments).HasForeignKey(a => a.ProfessionalId);
        builder.HasIndex(a => new { a.ProfessionalId, a.ScheduledAt });
    }
}

public class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.ToTable("medical_records");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.RecordNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(m => m.RecordNumber).IsUnique();
        builder.HasIndex(m => m.PatientId).IsUnique();
        builder.HasOne(m => m.Patient).WithOne(p => p.MedicalRecord).HasForeignKey<MedicalRecord>(m => m.PatientId);
    }
}

public class MedicalRecordEntryConfiguration : IEntityTypeConfiguration<MedicalRecordEntry>
{
    public void Configure(EntityTypeBuilder<MedicalRecordEntry> builder)
    {
        builder.ToTable("medical_record_entries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Content).HasMaxLength(8000).IsRequired();
        builder.Property(e => e.Cid10Code).HasMaxLength(10);
        builder.Property(e => e.ClientRequestId).HasMaxLength(64);
        builder.HasIndex(e => e.ClientRequestId).IsUnique().HasFilter("\"ClientRequestId\" IS NOT NULL");
        builder.Property(e => e.SignatureImage).HasColumnType("text");
        builder.Property(e => e.SignatureHash).HasMaxLength(128);
        builder.HasOne(e => e.MedicalRecord).WithMany(m => m.Entries).HasForeignKey(e => e.MedicalRecordId);
        builder.HasOne(e => e.Professional).WithMany(p => p.MedicalRecordEntries).HasForeignKey(e => e.ProfessionalId);
        builder.HasOne(e => e.Appointment).WithMany().HasForeignKey(e => e.AppointmentId);
        builder.HasOne(e => e.Hospitalization).WithMany().HasForeignKey(e => e.HospitalizationId);
        builder.HasOne(e => e.SignedByProfessional).WithMany().HasForeignKey(e => e.SignedByProfessionalId);
    }
}

public class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
{
    public void Configure(EntityTypeBuilder<FinancialAccount> builder)
    {
        builder.ToTable("financial_accounts");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Description).HasMaxLength(300).IsRequired();
        builder.Property(f => f.Notes).HasMaxLength(500);
        builder.Property(f => f.InvoiceNumber).HasMaxLength(60);
        builder.Property(f => f.Amount).HasPrecision(18, 2);
        builder.Property(f => f.PaidAmount).HasPrecision(18, 2);
        builder.Property(f => f.CounterpartyName).HasMaxLength(200);
        builder.HasOne(f => f.Patient).WithMany(p => p.FinancialAccounts).HasForeignKey(f => f.PatientId);
        builder.HasOne(f => f.Supplier).WithMany().HasForeignKey(f => f.SupplierId);
        builder.HasOne(f => f.Appointment).WithMany().HasForeignKey(f => f.AppointmentId);
        builder.HasOne(f => f.Hospitalization).WithMany().HasForeignKey(f => f.HospitalizationId);
        builder.HasOne(f => f.HealthInsurance).WithMany().HasForeignKey(f => f.HealthInsuranceId);
        builder.HasOne(f => f.TissGuide).WithMany().HasForeignKey(f => f.TissGuideId);
        builder.HasOne(f => f.ParentFinancialAccount)
            .WithMany(f => f.InstallmentAccounts)
            .HasForeignKey(f => f.ParentFinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(f => f.ParentFinancialAccountId);
        builder.HasIndex(f => f.AppointmentId).IsUnique().HasFilter("\"AppointmentId\" IS NOT NULL");
        builder.HasIndex(f => f.HospitalizationId).IsUnique().HasFilter("\"HospitalizationId\" IS NOT NULL");
        builder.HasIndex(f => f.TissGuideId).IsUnique().HasFilter("\"TissGuideId\" IS NOT NULL");
    }
}

public class FinancialPaymentConfiguration : IEntityTypeConfiguration<FinancialPayment>
{
    public void Configure(EntityTypeBuilder<FinancialPayment> builder)
    {
        builder.ToTable("financial_payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Notes).HasMaxLength(300);
        builder.HasIndex(p => new { p.FinancialAccountId, p.PaidAt });
        builder.HasOne(p => p.FinancialAccount)
            .WithMany(f => f.Payments)
            .HasForeignKey(p => p.FinancialAccountId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.PixCharge)
            .WithMany()
            .HasForeignKey(p => p.PixChargeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class FinancialPaymentInstallmentConfiguration : IEntityTypeConfiguration<FinancialPaymentInstallment>
{
    public void Configure(EntityTypeBuilder<FinancialPaymentInstallment> builder)
    {
        builder.ToTable("financial_payment_installments");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Amount).HasPrecision(18, 2);
        builder.HasIndex(i => new { i.FinancialPaymentId, i.InstallmentNumber }).IsUnique();
        builder.HasOne(i => i.FinancialPayment)
            .WithMany(p => p.Installments)
            .HasForeignKey(i => i.FinancialPaymentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.FinancialAccount)
            .WithMany()
            .HasForeignKey(i => i.FinancialAccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(200).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(u => u.MfaSecretEncrypted).HasMaxLength(512);
        builder.HasOne(u => u.Professional).WithMany().HasForeignKey(u => u.ProfessionalId);
        builder.HasOne(u => u.Patient).WithMany().HasForeignKey(u => u.PatientId);
    }
}
