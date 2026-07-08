using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class TussCatalog : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TussTableType TableType { get; set; }
    public string? Unit { get; set; }
    public decimal? ReferencePrice { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
}

public class SigtapProcedure : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Competence { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public string? Complexity { get; set; }
    public decimal? HospitalAmount { get; set; }
    public decimal? ProfessionalAmount { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
}

public class TissDemonstrativo : BaseEntity
{
    public Guid HealthInsuranceId { get; set; }
    public HealthInsurance HealthInsurance { get; set; } = null!;

    public string DemonstrativoNumber { get; set; } = string.Empty;
    public string Competence { get; set; } = string.Empty;
    public TissDemonstrativoStatus Status { get; set; } = TissDemonstrativoStatus.Imported;
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalGlosa { get; set; }
    public string? SourceFileName { get; set; }
    public string? RawContent { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public ICollection<TissDemonstrativoItem> Items { get; set; } = [];
}

public class TissDemonstrativoItem : BaseEntity
{
    public Guid TissDemonstrativoId { get; set; }
    public TissDemonstrativo TissDemonstrativo { get; set; } = null!;

    public Guid? TissGuideId { get; set; }
    public TissGuide? TissGuide { get; set; }

    public string GuideNumber { get; set; } = string.Empty;
    public string? TussCode { get; set; }
    public decimal BilledAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal GlosaAmount { get; set; }
    public string? GlosaReason { get; set; }
    public string? AnsGlosaCode { get; set; }
    public bool IsMatched { get; set; }
}

public class TissGuideAnnex : BaseEntity
{
    public Guid TissGuideId { get; set; }
    public TissGuide TissGuide { get; set; } = null!;

    public TissAnnexType AnnexType { get; set; }
    public string? Cid10Code { get; set; }
    public string? ClinicalIndication { get; set; }
    public string? CycleInfo { get; set; }
    public string? Notes { get; set; }
    public string? PayloadJson { get; set; }

    public ICollection<TissOpmeItem> OpmeItems { get; set; } = [];
}

public class TissOpmeItem : BaseEntity
{
    public Guid TissGuideAnnexId { get; set; }
    public TissGuideAnnex TissGuideAnnex { get; set; } = null!;

    public string TussCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? AuthorizationNumber { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}

public class CbhpmProcedure : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Port { get; set; }
    public string? Uco { get; set; }
    public decimal? ReferencePrice { get; set; }
    public DateOnly? ValidFrom { get; set; }
}

public class BrasindiceItem : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Laboratory { get; set; }
    public string? Presentation { get; set; }
    public decimal? ReferencePrice { get; set; }
    public DateOnly? ValidFrom { get; set; }
}

public class SimproItem : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Unit { get; set; }
    public decimal? ReferencePrice { get; set; }
    public DateOnly? ValidFrom { get; set; }
}

public class OperatorTransactionLog : BaseEntity
{
    public Guid HealthInsuranceId { get; set; }
    public HealthInsurance HealthInsurance { get; set; } = null!;

    public OperatorTransactionType TransactionType { get; set; }
    public OperatorTransactionStatus Status { get; set; }
    public string? ReferenceId { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public string? ErrorMessage { get; set; }
    public int? DurationMs { get; set; }
}
