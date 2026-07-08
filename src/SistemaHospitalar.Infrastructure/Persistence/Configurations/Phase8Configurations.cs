using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class InstrumentKitConfiguration : IEntityTypeConfiguration<InstrumentKit>
{
    public void Configure(EntityTypeBuilder<InstrumentKit> builder)
    {
        builder.ToTable("instrument_kits");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Name).HasMaxLength(200).IsRequired();
        builder.Property(k => k.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(k => k.Code).IsUnique();
        builder.Property(k => k.Description).HasMaxLength(500);
    }
}

public class SterilizationCycleConfiguration : IEntityTypeConfiguration<SterilizationCycle>
{
    public void Configure(EntityTypeBuilder<SterilizationCycle> builder)
    {
        builder.ToTable("sterilization_cycles");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.SterilizerName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.OperatorName).HasMaxLength(200);
        builder.HasOne(c => c.InstrumentKit).WithMany().HasForeignKey(c => c.InstrumentKitId);
    }
}

public class BloodUnitConfiguration : IEntityTypeConfiguration<BloodUnit>
{
    public void Configure(EntityTypeBuilder<BloodUnit> builder)
    {
        builder.ToTable("blood_units");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.UnitCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(b => b.UnitCode).IsUnique();
    }
}

public class TransfusionRequestConfiguration : IEntityTypeConfiguration<TransfusionRequest>
{
    public void Configure(EntityTypeBuilder<TransfusionRequest> builder)
    {
        builder.ToTable("transfusion_requests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.HasOne(r => r.Patient).WithMany().HasForeignKey(r => r.PatientId);
        builder.HasOne(r => r.Hospitalization).WithMany().HasForeignKey(r => r.HospitalizationId);
        builder.HasOne(r => r.RequestingProfessional).WithMany().HasForeignKey(r => r.RequestingProfessionalId);
        builder.HasOne(r => r.BloodUnit).WithMany().HasForeignKey(r => r.BloodUnitId);
    }
}

public class DialysisSessionConfiguration : IEntityTypeConfiguration<DialysisSession>
{
    public void Configure(EntityTypeBuilder<DialysisSession> builder)
    {
        builder.ToTable("dialysis_sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.MachineNumber).HasMaxLength(20).IsRequired();
        builder.Property(s => s.NurseName).HasMaxLength(200);
        builder.Property(s => s.Notes).HasMaxLength(500);
        builder.Property(s => s.DryWeightKg).HasPrecision(6, 2);
        builder.HasOne(s => s.Patient).WithMany().HasForeignKey(s => s.PatientId);
        builder.HasOne(s => s.Hospitalization).WithMany().HasForeignKey(s => s.HospitalizationId);
    }
}

public class LaundryBatchConfiguration : IEntityTypeConfiguration<LaundryBatch>
{
    public void Configure(EntityTypeBuilder<LaundryBatch> builder)
    {
        builder.ToTable("laundry_batches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BatchNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(b => b.BatchNumber).IsUnique();
        builder.Property(b => b.OriginDetail).HasMaxLength(200);
        builder.Property(b => b.WeightKg).HasPrecision(8, 2);
        builder.Property(b => b.Notes).HasMaxLength(500);
    }
}
