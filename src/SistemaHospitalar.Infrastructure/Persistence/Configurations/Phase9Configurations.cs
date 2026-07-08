using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class ChemotherapySessionConfiguration : IEntityTypeConfiguration<ChemotherapySession>
{
    public void Configure(EntityTypeBuilder<ChemotherapySession> builder)
    {
        builder.ToTable("chemotherapy_sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ProtocolName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.DrugRegimen).HasMaxLength(500).IsRequired();
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.HasOne(s => s.Patient).WithMany().HasForeignKey(s => s.PatientId);
        builder.HasOne(s => s.Professional).WithMany().HasForeignKey(s => s.ProfessionalId);
        builder.HasOne(s => s.Hospitalization).WithMany().HasForeignKey(s => s.HospitalizationId);
    }
}

public class PhysiotherapySessionConfiguration : IEntityTypeConfiguration<PhysiotherapySession>
{
    public void Configure(EntityTypeBuilder<PhysiotherapySession> builder)
    {
        builder.ToTable("physiotherapy_sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TherapistName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Goals).HasMaxLength(500);
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.HasOne(s => s.Patient).WithMany().HasForeignKey(s => s.PatientId);
        builder.HasOne(s => s.Hospitalization).WithMany().HasForeignKey(s => s.HospitalizationId);
    }
}

public class TelemedicineAppointmentConfiguration : IEntityTypeConfiguration<TelemedicineAppointment>
{
    public void Configure(EntityTypeBuilder<TelemedicineAppointment> builder)
    {
        builder.ToTable("telemedicine_appointments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.MeetingUrl).HasMaxLength(500);
        builder.Property(a => a.ChiefComplaint).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(1000);
        builder.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId);
        builder.HasOne(a => a.Professional).WithMany().HasForeignKey(a => a.ProfessionalId);
    }
}

public class InfectionSurveillanceConfiguration : IEntityTypeConfiguration<InfectionSurveillance>
{
    public void Configure(EntityTypeBuilder<InfectionSurveillance> builder)
    {
        builder.ToTable("infection_surveillance");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Location).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Organism).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Site).HasMaxLength(200);
        builder.Property(i => i.ReportedBy).HasMaxLength(200);
        builder.Property(i => i.Notes).HasMaxLength(1000);
        builder.HasOne(i => i.Patient).WithMany().HasForeignKey(i => i.PatientId);
        builder.HasOne(i => i.Hospitalization).WithMany().HasForeignKey(i => i.HospitalizationId);
    }
}

public class IsolationPrecautionConfiguration : IEntityTypeConfiguration<IsolationPrecaution>
{
    public void Configure(EntityTypeBuilder<IsolationPrecaution> builder)
    {
        builder.ToTable("isolation_precautions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Reason).HasMaxLength(500).IsRequired();
        builder.HasOne(p => p.Patient).WithMany().HasForeignKey(p => p.PatientId);
        builder.HasOne(p => p.Hospitalization).WithMany().HasForeignKey(p => p.HospitalizationId);
    }
}
