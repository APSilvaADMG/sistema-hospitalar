using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class VitalSignRecordConfiguration : IEntityTypeConfiguration<VitalSignRecord>
{
    public void Configure(EntityTypeBuilder<VitalSignRecord> builder)
    {
        builder.ToTable("vital_sign_records");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Temperature).HasPrecision(4, 1);
        builder.Property(v => v.Notes).HasMaxLength(500);
        builder.HasOne(v => v.Hospitalization).WithMany().HasForeignKey(v => v.HospitalizationId);
        builder.HasOne(v => v.RecordedByProfessional).WithMany().HasForeignKey(v => v.RecordedByProfessionalId);
    }
}

public class AmbulanceConfiguration : IEntityTypeConfiguration<Ambulance>
{
    public void Configure(EntityTypeBuilder<Ambulance> builder)
    {
        builder.ToTable("ambulances");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(a => a.Code).IsUnique();
        builder.Property(a => a.Plate).HasMaxLength(10).IsRequired();
        builder.Property(a => a.BaseLocation).HasMaxLength(200);
    }
}

public class AmbulanceDispatchConfiguration : IEntityTypeConfiguration<AmbulanceDispatch>
{
    public void Configure(EntityTypeBuilder<AmbulanceDispatch> builder)
    {
        builder.ToTable("ambulance_dispatches");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.PatientName).HasMaxLength(200).IsRequired();
        builder.Property(d => d.PickupAddress).HasMaxLength(300).IsRequired();
        builder.Property(d => d.Destination).HasMaxLength(300).IsRequired();
        builder.Property(d => d.Notes).HasMaxLength(1000);
        builder.HasOne(d => d.Ambulance).WithMany().HasForeignKey(d => d.AmbulanceId);
    }
}

public class ParkingZoneConfiguration : IEntityTypeConfiguration<ParkingZone>
{
    public void Configure(EntityTypeBuilder<ParkingZone> builder)
    {
        builder.ToTable("parking_zones");
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Name).HasMaxLength(100).IsRequired();
        builder.Property(z => z.HourlyRate).HasPrecision(18, 2);
        builder.Property(z => z.Description).HasMaxLength(300);
    }
}

public class ParkingSessionConfiguration : IEntityTypeConfiguration<ParkingSession>
{
    public void Configure(EntityTypeBuilder<ParkingSession> builder)
    {
        builder.ToTable("parking_sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.VehiclePlate).HasMaxLength(10).IsRequired();
        builder.Property(s => s.AmountCharged).HasPrecision(18, 2);
        builder.HasOne(s => s.ParkingZone).WithMany().HasForeignKey(s => s.ParkingZoneId);
        builder.HasOne(s => s.Patient).WithMany().HasForeignKey(s => s.PatientId);
    }
}

public class DietOrderConfiguration : IEntityTypeConfiguration<DietOrder>
{
    public void Configure(EntityTypeBuilder<DietOrder> builder)
    {
        builder.ToTable("diet_orders");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Notes).HasMaxLength(500);
        builder.HasOne(d => d.Hospitalization).WithMany().HasForeignKey(d => d.HospitalizationId);
    }
}
