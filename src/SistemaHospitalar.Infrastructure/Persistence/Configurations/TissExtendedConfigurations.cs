using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class TussCatalogConfiguration : IEntityTypeConfiguration<TussCatalog>
{
    public void Configure(EntityTypeBuilder<TussCatalog> builder)
    {
        builder.ToTable("tuss_catalogs");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(t => new { t.Code, t.TableType }).IsUnique();
        builder.Property(t => t.Description).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Unit).HasMaxLength(20);
        builder.Property(t => t.ReferencePrice).HasPrecision(18, 2);
    }
}

public class SigtapProcedureConfiguration : IEntityTypeConfiguration<SigtapProcedure>
{
    public void Configure(EntityTypeBuilder<SigtapProcedure> builder)
    {
        builder.ToTable("sigtap_procedures");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Competence).HasMaxLength(7).IsRequired();
        builder.HasIndex(s => new { s.Code, s.Competence }).IsUnique();
        builder.Property(s => s.Description).HasMaxLength(400).IsRequired();
        builder.Property(s => s.GroupName).HasMaxLength(120);
        builder.Property(s => s.Complexity).HasMaxLength(20);
        builder.Property(s => s.HospitalAmount).HasPrecision(18, 2);
        builder.Property(s => s.ProfessionalAmount).HasPrecision(18, 2);
    }
}

public class TissDemonstrativoConfiguration : IEntityTypeConfiguration<TissDemonstrativo>
{
    public void Configure(EntityTypeBuilder<TissDemonstrativo> builder)
    {
        builder.ToTable("tiss_demonstrativos");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DemonstrativoNumber).HasMaxLength(40).IsRequired();
        builder.Property(d => d.Competence).HasMaxLength(7).IsRequired();
        builder.Property(d => d.SourceFileName).HasMaxLength(200);
        builder.Property(d => d.TotalBilled).HasPrecision(18, 2);
        builder.Property(d => d.TotalPaid).HasPrecision(18, 2);
        builder.Property(d => d.TotalGlosa).HasPrecision(18, 2);
        builder.HasOne(d => d.HealthInsurance).WithMany().HasForeignKey(d => d.HealthInsuranceId);
    }
}

public class TissDemonstrativoItemConfiguration : IEntityTypeConfiguration<TissDemonstrativoItem>
{
    public void Configure(EntityTypeBuilder<TissDemonstrativoItem> builder)
    {
        builder.ToTable("tiss_demonstrativo_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.GuideNumber).HasMaxLength(30).IsRequired();
        builder.Property(i => i.TussCode).HasMaxLength(20);
        builder.Property(i => i.BilledAmount).HasPrecision(18, 2);
        builder.Property(i => i.PaidAmount).HasPrecision(18, 2);
        builder.Property(i => i.GlosaAmount).HasPrecision(18, 2);
        builder.Property(i => i.GlosaReason).HasMaxLength(500);
        builder.Property(i => i.AnsGlosaCode).HasMaxLength(20);
        builder.HasOne(i => i.TissDemonstrativo).WithMany(d => d.Items).HasForeignKey(i => i.TissDemonstrativoId);
        builder.HasOne(i => i.TissGuide).WithMany().HasForeignKey(i => i.TissGuideId);
    }
}

public class TissGuideAnnexConfiguration : IEntityTypeConfiguration<TissGuideAnnex>
{
    public void Configure(EntityTypeBuilder<TissGuideAnnex> builder)
    {
        builder.ToTable("tiss_guide_annexes");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Cid10Code).HasMaxLength(10);
        builder.Property(a => a.ClinicalIndication).HasMaxLength(500);
        builder.Property(a => a.CycleInfo).HasMaxLength(200);
        builder.Property(a => a.Notes).HasMaxLength(1000);
        builder.HasOne(a => a.TissGuide).WithMany(g => g.Annexes).HasForeignKey(a => a.TissGuideId);
    }
}

public class TissOpmeItemConfiguration : IEntityTypeConfiguration<TissOpmeItem>
{
    public void Configure(EntityTypeBuilder<TissOpmeItem> builder)
    {
        builder.ToTable("tiss_opme_items");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.TussCode).HasMaxLength(20).IsRequired();
        builder.Property(o => o.Description).HasMaxLength(300).IsRequired();
        builder.Property(o => o.Manufacturer).HasMaxLength(120);
        builder.Property(o => o.AuthorizationNumber).HasMaxLength(40);
        builder.Property(o => o.UnitPrice).HasPrecision(18, 2);
        builder.HasOne(o => o.TissGuideAnnex).WithMany(a => a.OpmeItems).HasForeignKey(o => o.TissGuideAnnexId);
    }
}

public class CbhpmProcedureConfiguration : IEntityTypeConfiguration<CbhpmProcedure>
{
    public void Configure(EntityTypeBuilder<CbhpmProcedure> builder)
    {
        builder.ToTable("cbhpm_procedures");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.Description).HasMaxLength(400).IsRequired();
        builder.Property(c => c.Port).HasMaxLength(10);
        builder.Property(c => c.Uco).HasMaxLength(10);
        builder.Property(c => c.ReferencePrice).HasPrecision(18, 2);
    }
}

public class BrasindiceItemConfiguration : IEntityTypeConfiguration<BrasindiceItem>
{
    public void Configure(EntityTypeBuilder<BrasindiceItem> builder)
    {
        builder.ToTable("brasindice_items");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(b => b.Code).IsUnique();
        builder.Property(b => b.Description).HasMaxLength(400).IsRequired();
        builder.Property(b => b.Laboratory).HasMaxLength(120);
        builder.Property(b => b.Presentation).HasMaxLength(80);
        builder.Property(b => b.ReferencePrice).HasPrecision(18, 2);
    }
}

public class SimproItemConfiguration : IEntityTypeConfiguration<SimproItem>
{
    public void Configure(EntityTypeBuilder<SimproItem> builder)
    {
        builder.ToTable("simpro_items");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.Description).HasMaxLength(400).IsRequired();
        builder.Property(s => s.Manufacturer).HasMaxLength(120);
        builder.Property(s => s.Unit).HasMaxLength(20);
        builder.Property(s => s.ReferencePrice).HasPrecision(18, 2);
    }
}

public class OperatorTransactionLogConfiguration : IEntityTypeConfiguration<OperatorTransactionLog>
{
    public void Configure(EntityTypeBuilder<OperatorTransactionLog> builder)
    {
        builder.ToTable("operator_transaction_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ReferenceId).HasMaxLength(60);
        builder.Property(l => l.ErrorMessage).HasMaxLength(500);
        builder.HasOne(l => l.HealthInsurance).WithMany().HasForeignKey(l => l.HealthInsuranceId);
    }
}
