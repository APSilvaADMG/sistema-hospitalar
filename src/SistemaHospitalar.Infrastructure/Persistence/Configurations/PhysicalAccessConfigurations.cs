using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class AccessZoneConfiguration : IEntityTypeConfiguration<AccessZone>
{
    public void Configure(EntityTypeBuilder<AccessZone> builder)
    {
        builder.ToTable("access_zones");
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(z => z.Code).IsUnique();
        builder.Property(z => z.Name).HasMaxLength(120).IsRequired();
        builder.Property(z => z.Building).HasMaxLength(80);
        builder.Property(z => z.Floor).HasMaxLength(20);
    }
}

public class AccessTurnstileConfiguration : IEntityTypeConfiguration<AccessTurnstile>
{
    public void Configure(EntityTypeBuilder<AccessTurnstile> builder)
    {
        builder.ToTable("access_turnstiles");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(t => t.Code).IsUnique();
        builder.Property(t => t.Name).HasMaxLength(120).IsRequired();
        builder.Property(t => t.IntegrationVendor).HasMaxLength(80);
        builder.HasOne(t => t.AccessZone).WithMany().HasForeignKey(t => t.AccessZoneId);
    }
}

public class AccessCredentialConfiguration : IEntityTypeConfiguration<AccessCredential>
{
    public void Configure(EntityTypeBuilder<AccessCredential> builder)
    {
        builder.ToTable("access_credentials");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.HolderName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Token).HasMaxLength(120).IsRequired();
        builder.HasIndex(c => c.Token);
        builder.HasOne(c => c.Patient).WithMany().HasForeignKey(c => c.PatientId);
        builder.HasOne(c => c.Employee).WithMany().HasForeignKey(c => c.EmployeeId);
        builder.HasOne(c => c.VisitorLog).WithMany().HasForeignKey(c => c.VisitorLogId);
        builder.HasOne(c => c.AllowedZone).WithMany().HasForeignKey(c => c.AllowedZoneId);
    }
}

public class AccessControlRecordConfiguration : IEntityTypeConfiguration<AccessControlRecord>
{
    public void Configure(EntityTypeBuilder<AccessControlRecord> builder)
    {
        builder.ToTable("access_control_records");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.PersonName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Location).HasMaxLength(120);
        builder.Property(r => r.Details).HasMaxLength(500);
        builder.HasIndex(r => r.OccurredAt);
        builder.HasOne(r => r.AccessZone).WithMany().HasForeignKey(r => r.AccessZoneId);
        builder.HasOne(r => r.Turnstile).WithMany().HasForeignKey(r => r.TurnstileId);
    }
}

public class FacialBiometricTemplateConfiguration : IEntityTypeConfiguration<FacialBiometricTemplate>
{
    public void Configure(EntityTypeBuilder<FacialBiometricTemplate> builder)
    {
        builder.ToTable("facial_biometric_templates");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.PersonName).HasMaxLength(200).IsRequired();
        builder.Property(f => f.TemplateHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(f => f.TemplateHash);
        builder.HasOne(f => f.Patient).WithMany().HasForeignKey(f => f.PatientId);
        builder.HasOne(f => f.Employee).WithMany().HasForeignKey(f => f.EmployeeId);
        builder.HasOne(f => f.Professional).WithMany().HasForeignKey(f => f.ProfessionalId);
    }
}

public class RegisteredVehicleConfiguration : IEntityTypeConfiguration<RegisteredVehicle>
{
    public void Configure(EntityTypeBuilder<RegisteredVehicle> builder)
    {
        builder.ToTable("registered_vehicles");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Plate).HasMaxLength(10).IsRequired();
        builder.HasIndex(v => v.Plate).IsUnique();
        builder.Property(v => v.Model).HasMaxLength(80);
        builder.Property(v => v.Color).HasMaxLength(40);
        builder.Property(v => v.OwnerName).HasMaxLength(200).IsRequired();
        builder.HasOne(v => v.Patient).WithMany().HasForeignKey(v => v.PatientId);
        builder.HasOne(v => v.Employee).WithMany().HasForeignKey(v => v.EmployeeId);
    }
}

public class LprReadEventConfiguration : IEntityTypeConfiguration<LprReadEvent>
{
    public void Configure(EntityTypeBuilder<LprReadEvent> builder)
    {
        builder.ToTable("lpr_read_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Plate).HasMaxLength(10).IsRequired();
        builder.Property(e => e.CameraLocation).HasMaxLength(120).IsRequired();
        builder.HasIndex(e => e.ReadAt);
        builder.HasOne(e => e.RegisteredVehicle).WithMany().HasForeignKey(e => e.RegisteredVehicleId);
        builder.HasOne(e => e.ParkingSession).WithMany().HasForeignKey(e => e.ParkingSessionId);
    }
}

public class KioskTicketConfiguration : IEntityTypeConfiguration<KioskTicket>
{
    public void Configure(EntityTypeBuilder<KioskTicket> builder)
    {
        builder.ToTable("kiosk_tickets");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TicketNumber).HasMaxLength(20).IsRequired();
        builder.Property(t => t.PatientName).HasMaxLength(200);
        builder.Property(t => t.Sector).HasMaxLength(120);
        builder.HasIndex(t => t.IssuedAt);
    }
}
