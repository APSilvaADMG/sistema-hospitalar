using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class LabExamCatalogConfiguration : IEntityTypeConfiguration<LabExamCatalog>
{
    public void Configure(EntityTypeBuilder<LabExamCatalog> builder)
    {
        builder.ToTable("lab_exam_catalogs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TussCode).HasMaxLength(20);
        builder.Property(e => e.SampleType).HasMaxLength(50);
        builder.Property(e => e.ReferenceRange).HasMaxLength(100);
        builder.Property(e => e.Unit).HasMaxLength(20);
        builder.Property(e => e.Category).HasMaxLength(80);
    }
}

public class LabOrderConfiguration : IEntityTypeConfiguration<LabOrder>
{
    public void Configure(EntityTypeBuilder<LabOrder> builder)
    {
        builder.ToTable("lab_orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.HasOne(o => o.Patient).WithMany().HasForeignKey(o => o.PatientId);
        builder.HasOne(o => o.RequestingProfessional).WithMany().HasForeignKey(o => o.RequestingProfessionalId);
    }
}

public class LabOrderItemConfiguration : IEntityTypeConfiguration<LabOrderItem>
{
    public void Configure(EntityTypeBuilder<LabOrderItem> builder)
    {
        builder.ToTable("lab_order_items");
        builder.HasKey(i => i.Id);
        builder.HasOne(i => i.LabOrder).WithMany(o => o.Items).HasForeignKey(i => i.LabOrderId);
        builder.HasOne(i => i.LabExamCatalog).WithMany(e => e.OrderItems).HasForeignKey(i => i.LabExamCatalogId);
        builder.HasOne(i => i.Result).WithOne(r => r.LabOrderItem).HasForeignKey<LabResult>(r => r.LabOrderItemId);
    }
}

public class LabResultConfiguration : IEntityTypeConfiguration<LabResult>
{
    public void Configure(EntityTypeBuilder<LabResult> builder)
    {
        builder.ToTable("lab_results");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Value).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Unit).HasMaxLength(20);
        builder.Property(r => r.ReferenceRange).HasMaxLength(100);
        builder.Property(r => r.Notes).HasMaxLength(500);
    }
}

public class ImagingStudyConfiguration : IEntityTypeConfiguration<ImagingStudy>
{
    public void Configure(EntityTypeBuilder<ImagingStudy> builder)
    {
        builder.ToTable("imaging_studies");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.StudyDescription).HasMaxLength(300).IsRequired();
        builder.Property(s => s.ReportContent).HasMaxLength(8000);
        builder.Property(s => s.AccessionNumber).HasMaxLength(30);
        builder.HasIndex(s => s.AccessionNumber).IsUnique();
        builder.HasOne(s => s.Patient).WithMany().HasForeignKey(s => s.PatientId);
        builder.HasOne(s => s.RequestingProfessional).WithMany().HasForeignKey(s => s.RequestingProfessionalId);
        builder.HasOne(s => s.ReportingProfessional).WithMany().HasForeignKey(s => s.ReportingProfessionalId);
    }
}

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).HasMaxLength(150).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(500);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FullName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Cpf).HasMaxLength(11);
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.SocialName).HasMaxLength(200);
        builder.Property(e => e.Rg).HasMaxLength(20);
        builder.Property(e => e.MobilePhone).HasMaxLength(20);
        builder.Property(e => e.JobTitle).HasMaxLength(120);
        builder.Property(e => e.AddressStreet).HasMaxLength(200);
        builder.Property(e => e.AddressNumber).HasMaxLength(20);
        builder.Property(e => e.AddressComplement).HasMaxLength(100);
        builder.Property(e => e.AddressNeighborhood).HasMaxLength(100);
        builder.Property(e => e.AddressCity).HasMaxLength(100);
        builder.Property(e => e.AddressState).HasMaxLength(2);
        builder.Property(e => e.AddressZipCode).HasMaxLength(8);
        builder.Property(e => e.EmergencyContactName).HasMaxLength(200);
        builder.Property(e => e.EmergencyContactPhone).HasMaxLength(20);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.Property(e => e.PayrollNotes).HasMaxLength(500);
        builder.Property(e => e.BaseSalary).HasPrecision(18, 2);
        builder.Property(e => e.PhotoData).HasColumnType("text");
        builder.HasOne(e => e.Department).WithMany(d => d.Employees).HasForeignKey(e => e.DepartmentId);
    }
}

public class EmployeeShiftConfiguration : IEntityTypeConfiguration<EmployeeShift>
{
    public void Configure(EntityTypeBuilder<EmployeeShift> builder)
    {
        builder.ToTable("employee_shifts");
        builder.HasKey(s => s.Id);
        builder.HasOne(s => s.Employee).WithMany(e => e.Shifts).HasForeignKey(s => s.EmployeeId);
        builder.HasOne(s => s.Department).WithMany().HasForeignKey(s => s.DepartmentId);
        builder.HasIndex(s => new { s.EmployeeId, s.ShiftDate, s.ShiftType }).IsUnique();
    }
}

public class EmployeeHrEventConfiguration : IEntityTypeConfiguration<EmployeeHrEvent>
{
    public void Configure(EntityTypeBuilder<EmployeeHrEvent> builder)
    {
        builder.ToTable("employee_hr_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Detail).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.HasOne(e => e.Employee).WithMany(emp => emp.HrEvents).HasForeignKey(e => e.EmployeeId);
        builder.HasIndex(e => new { e.EventType, e.StartDate });
    }
}

public class TissGuideConfiguration : IEntityTypeConfiguration<TissGuide>
{
    public void Configure(EntityTypeBuilder<TissGuide> builder)
    {
        builder.ToTable("tiss_guides");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.GuideNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(g => g.GuideNumber).IsUnique();
        builder.Property(g => g.TotalAmount).HasPrecision(18, 2);
        builder.Property(g => g.Notes).HasMaxLength(1000);
        builder.Property(g => g.ClientRequestId).HasMaxLength(64);
        builder.Property(g => g.BeneficiaryCardNumber).HasMaxLength(40);
        builder.Property(g => g.BeneficiaryPlanName).HasMaxLength(120);
        builder.Property(g => g.BeneficiaryCns).HasMaxLength(20);
        builder.Property(g => g.BeneficiaryAccommodation).HasMaxLength(40);
        builder.Property(g => g.AuthorizationPassword).HasMaxLength(40);
        builder.Property(g => g.Cid10Code).HasMaxLength(10);
        builder.Property(g => g.Cid10Secondary).HasMaxLength(10);
        builder.Property(g => g.ClinicalJustification).HasMaxLength(2000);
        builder.Property(g => g.RequestingProfessionalName).HasMaxLength(200);
        builder.Property(g => g.RequestingProfessionalCrm).HasMaxLength(30);
        builder.Property(g => g.ExecutingProfessionalName).HasMaxLength(200);
        builder.Property(g => g.ExecutingProfessionalCrm).HasMaxLength(30);
        builder.Property(g => g.RequestedBedType).HasMaxLength(40);
        builder.Property(g => g.ParticipationPercent).HasPrecision(5, 2);
        builder.HasIndex(g => g.ClientRequestId).IsUnique().HasFilter("\"ClientRequestId\" IS NOT NULL");
        builder.HasOne(g => g.Patient).WithMany().HasForeignKey(g => g.PatientId);
        builder.HasOne(g => g.HealthInsurance).WithMany().HasForeignKey(g => g.HealthInsuranceId);
        builder.HasOne(g => g.Appointment).WithMany().HasForeignKey(g => g.AppointmentId);
        builder.HasOne(g => g.Hospitalization).WithMany().HasForeignKey(g => g.HospitalizationId);
        builder.HasOne(g => g.TissBatch).WithMany(b => b.Guides).HasForeignKey(g => g.TissBatchId);
        builder.HasOne(g => g.RequestingProfessional).WithMany().HasForeignKey(g => g.RequestingProfessionalId);
        builder.HasOne(g => g.ExecutingProfessional).WithMany().HasForeignKey(g => g.ExecutingProfessionalId);
        builder.HasOne(g => g.ParentGuide).WithMany().HasForeignKey(g => g.ParentGuideId);
        builder.HasOne(g => g.Surgery).WithMany().HasForeignKey(g => g.SurgeryId);
        builder.HasOne(g => g.ServiceUnit).WithMany().HasForeignKey(g => g.ServiceUnitId);
    }
}

public class TissGuideItemConfiguration : IEntityTypeConfiguration<TissGuideItem>
{
    public void Configure(EntityTypeBuilder<TissGuideItem> builder)
    {
        builder.ToTable("tiss_guide_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.TussCode).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(300).IsRequired();
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.Cid10Code).HasMaxLength(10);
        builder.Property(i => i.RelatedTussCode).HasMaxLength(20);
        builder.HasOne(i => i.TissGuide).WithMany(g => g.Items).HasForeignKey(i => i.TissGuideId);
    }
}

public class TissGlosaConfiguration : IEntityTypeConfiguration<TissGlosa>
{
    public void Configure(EntityTypeBuilder<TissGlosa> builder)
    {
        builder.ToTable("tiss_glosas");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Reason).HasMaxLength(500).IsRequired();
        builder.Property(g => g.AnsGlosaCode).HasMaxLength(20);
        builder.Property(g => g.ContestationNotes).HasMaxLength(2000);
        builder.Property(g => g.GlosaAmount).HasPrecision(18, 2);
        builder.HasOne(g => g.TissGuide).WithMany(tg => tg.Glosas).HasForeignKey(g => g.TissGuideId);
        builder.HasOne(g => g.TissGuideItem).WithMany().HasForeignKey(g => g.TissGuideItemId);
    }
}

public class InsuranceEligibilityCheckConfiguration : IEntityTypeConfiguration<InsuranceEligibilityCheck>
{
    public void Configure(EntityTypeBuilder<InsuranceEligibilityCheck> builder)
    {
        builder.ToTable("insurance_eligibility_checks");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CardNumber).HasMaxLength(40).IsRequired();
        builder.Property(e => e.PlanName).HasMaxLength(120);
        builder.Property(e => e.CoverageSummary).HasMaxLength(500);
        builder.Property(e => e.ResponseMessage).HasMaxLength(500);
        builder.HasOne(e => e.Patient).WithMany().HasForeignKey(e => e.PatientId);
        builder.HasOne(e => e.HealthInsurance).WithMany().HasForeignKey(e => e.HealthInsuranceId);
    }
}

public class InsuranceAuthorizationConfiguration : IEntityTypeConfiguration<InsuranceAuthorization>
{
    public void Configure(EntityTypeBuilder<InsuranceAuthorization> builder)
    {
        builder.ToTable("insurance_authorizations");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AuthorizationNumber).HasMaxLength(40).IsRequired();
        builder.Property(a => a.ProcedureSummary).HasMaxLength(500);
        builder.Property(a => a.Notes).HasMaxLength(1000);
        builder.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId);
        builder.HasOne(a => a.HealthInsurance).WithMany().HasForeignKey(a => a.HealthInsuranceId);
        builder.HasOne(a => a.TissGuide).WithMany().HasForeignKey(a => a.TissGuideId);
    }
}

public class TissClinicalSourceConfiguration : IEntityTypeConfiguration<TissClinicalSource>
{
    public void Configure(EntityTypeBuilder<TissClinicalSource> builder)
    {
        builder.ToTable("tiss_clinical_sources");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Label).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ReportCode).HasMaxLength(64);
        builder.Property(s => s.FormDataJson).HasColumnType("text").IsRequired();
        builder.Property(s => s.GeneratedArtifactJson).HasColumnType("text");
        builder.HasOne(s => s.Patient).WithMany().HasForeignKey(s => s.PatientId);
        builder.HasOne(s => s.HealthInsurance).WithMany().HasForeignKey(s => s.HealthInsuranceId);
        builder.HasOne(s => s.Appointment).WithMany().HasForeignKey(s => s.AppointmentId);
        builder.HasOne(s => s.Hospitalization).WithMany().HasForeignKey(s => s.HospitalizationId);
        builder.HasOne(s => s.ChemotherapySession).WithMany().HasForeignKey(s => s.ChemotherapySessionId);
        builder.HasOne(s => s.Surgery).WithMany().HasForeignKey(s => s.SurgeryId);
        builder.HasOne(s => s.LabOrder).WithMany().HasForeignKey(s => s.LabOrderId);
        builder.HasOne(s => s.ImagingStudy).WithMany().HasForeignKey(s => s.ImagingStudyId);
        builder.HasOne(s => s.GeneratedTissGuide).WithMany().HasForeignKey(s => s.GeneratedTissGuideId);
    }
}

public class TissBatchConfiguration : IEntityTypeConfiguration<TissBatch>
{
    public void Configure(EntityTypeBuilder<TissBatch> builder)
    {
        builder.ToTable("tiss_batches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BatchNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(b => b.BatchNumber).IsUnique();
        builder.Property(b => b.Competence).HasMaxLength(7).IsRequired();
        builder.Property(b => b.ProtocolNumber).HasMaxLength(40);
        builder.Property(b => b.TotalAmount).HasPrecision(18, 2);
        builder.HasOne(b => b.HealthInsurance).WithMany().HasForeignKey(b => b.HealthInsuranceId);
    }
}
