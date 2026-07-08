using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class ConsultingRoomConfiguration : IEntityTypeConfiguration<ConsultingRoom>
{
    public void Configure(EntityTypeBuilder<ConsultingRoom> builder)
    {
        builder.ToTable("consulting_rooms");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Floor).HasMaxLength(20);
        builder.Property(r => r.Building).HasMaxLength(100);
        builder.HasOne(r => r.Specialty).WithMany().HasForeignKey(r => r.SpecialtyId);
    }
}

public class ConsultingRoomScheduleConfiguration : IEntityTypeConfiguration<ConsultingRoomSchedule>
{
    public void Configure(EntityTypeBuilder<ConsultingRoomSchedule> builder)
    {
        builder.ToTable("consulting_room_schedules");
        builder.HasKey(s => s.Id);
        builder.HasOne(s => s.ConsultingRoom).WithMany().HasForeignKey(s => s.ConsultingRoomId);
        builder.HasOne(s => s.Professional).WithMany().HasForeignKey(s => s.ProfessionalId);
    }
}

public class HospitalityRoomConfiguration : IEntityTypeConfiguration<HospitalityRoom>
{
    public void Configure(EntityTypeBuilder<HospitalityRoom> builder)
    {
        builder.ToTable("hospitality_rooms");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RoomNumber).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Floor).HasMaxLength(20);
        builder.Property(r => r.DailyRate).HasPrecision(18, 2);
    }
}

public class HospitalityBookingConfiguration : IEntityTypeConfiguration<HospitalityBooking>
{
    public void Configure(EntityTypeBuilder<HospitalityBooking> builder)
    {
        builder.ToTable("hospitality_bookings");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.GuestName).HasMaxLength(200).IsRequired();
        builder.Property(b => b.GuestDocument).HasMaxLength(20);
        builder.Property(b => b.GuestPhone).HasMaxLength(20);
        builder.Property(b => b.Notes).HasMaxLength(500);
        builder.HasOne(b => b.HospitalityRoom).WithMany().HasForeignKey(b => b.HospitalityRoomId);
        builder.HasOne(b => b.Patient).WithMany().HasForeignKey(b => b.PatientId);
    }
}

public class MedicalEquipmentConfiguration : IEntityTypeConfiguration<MedicalEquipment>
{
    public void Configure(EntityTypeBuilder<MedicalEquipment> builder)
    {
        builder.ToTable("medical_equipment");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.AssetTag).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.AssetTag).IsUnique();
        builder.Property(e => e.Manufacturer).HasMaxLength(100);
        builder.Property(e => e.Model).HasMaxLength(100);
        builder.Property(e => e.Location).HasMaxLength(200);
    }
}

public class MaintenanceWorkOrderConfiguration : IEntityTypeConfiguration<MaintenanceWorkOrder>
{
    public void Configure(EntityTypeBuilder<MaintenanceWorkOrder> builder)
    {
        builder.ToTable("maintenance_work_orders");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Title).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(1000).IsRequired();
        builder.Property(w => w.TechnicianName).HasMaxLength(200);
        builder.HasOne(w => w.MedicalEquipment).WithMany().HasForeignKey(w => w.MedicalEquipmentId);
    }
}

public class SecurityIncidentConfiguration : IEntityTypeConfiguration<SecurityIncident>
{
    public void Configure(EntityTypeBuilder<SecurityIncident> builder)
    {
        builder.ToTable("security_incidents");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Location).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(1000).IsRequired();
        builder.Property(i => i.ReportedBy).HasMaxLength(200);
        builder.Property(i => i.ResolutionNotes).HasMaxLength(1000);
        builder.HasOne(i => i.Patient).WithMany().HasForeignKey(i => i.PatientId);
    }
}

public class VisitorLogConfiguration : IEntityTypeConfiguration<VisitorLog>
{
    public void Configure(EntityTypeBuilder<VisitorLog> builder)
    {
        builder.ToTable("visitor_logs");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.VisitorName).HasMaxLength(200).IsRequired();
        builder.Property(v => v.DocumentNumber).HasMaxLength(20);
        builder.Property(v => v.Destination).HasMaxLength(200);
        builder.Property(v => v.BadgeNumber).HasMaxLength(20);
        builder.Property(v => v.PhotoData).HasColumnType("text");
        builder.HasOne(v => v.Patient).WithMany().HasForeignKey(v => v.PatientId);
    }
}
