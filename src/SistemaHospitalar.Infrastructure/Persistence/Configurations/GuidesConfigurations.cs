using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class ServiceUnitConfiguration : IEntityTypeConfiguration<ServiceUnit>
{
    public void Configure(EntityTypeBuilder<ServiceUnit> builder)
    {
        builder.ToTable("service_units");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Name).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(u => u.Code).IsUnique();
        builder.Property(u => u.Cnes).HasMaxLength(20);
        builder.Property(u => u.Address).HasMaxLength(300);
    }
}

public class SusGuideConfiguration : IEntityTypeConfiguration<SusGuide>
{
    public void Configure(EntityTypeBuilder<SusGuide> builder)
    {
        builder.ToTable("sus_guides");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.GuideNumber).HasMaxLength(40).IsRequired();
        builder.HasIndex(g => g.GuideNumber).IsUnique();
        builder.Property(g => g.Cid10Code).HasMaxLength(10);
        builder.Property(g => g.SigtapProcedureCode).HasMaxLength(20);
        builder.Property(g => g.ProcedureDescription).HasMaxLength(300);
        builder.Property(g => g.Competence).HasMaxLength(6);
        builder.Property(g => g.AuthorizationNumber).HasMaxLength(40);
        builder.Property(g => g.Notes).HasMaxLength(1000);
        builder.Property(g => g.TotalAmount).HasPrecision(18, 2);
        builder.HasOne(g => g.Patient).WithMany().HasForeignKey(g => g.PatientId);
        builder.HasOne(g => g.Professional).WithMany().HasForeignKey(g => g.ProfessionalId);
        builder.HasOne(g => g.ServiceUnit).WithMany().HasForeignKey(g => g.ServiceUnitId);
        builder.HasOne(g => g.Appointment).WithMany().HasForeignKey(g => g.AppointmentId);
        builder.HasOne(g => g.Hospitalization).WithMany().HasForeignKey(g => g.HospitalizationId);
    }
}
