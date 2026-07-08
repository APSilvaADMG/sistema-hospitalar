using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class LabExamCatalog : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? TussCode { get; set; }
    public string? SampleType { get; set; }
    public string? ReferenceRange { get; set; }
    public string? Unit { get; set; }
    public string? Category { get; set; }
    public bool IsGeneral { get; set; }

    public ICollection<LabOrderItem> OrderItems { get; set; } = [];
    public ICollection<LabExamCatalogSpecialty> SpecialtyLinks { get; set; } = [];
}

public class LabOrder : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid RequestingProfessionalId { get; set; }
    public Professional RequestingProfessional { get; set; } = null!;

    public LabOrderStatus Status { get; set; } = LabOrderStatus.Requested;
    public string? Notes { get; set; }

    public ICollection<LabOrderItem> Items { get; set; } = [];
}

public class LabOrderItem : BaseEntity
{
    public Guid LabOrderId { get; set; }
    public LabOrder LabOrder { get; set; } = null!;

    public Guid LabExamCatalogId { get; set; }
    public LabExamCatalog LabExamCatalog { get; set; } = null!;

    public LabItemStatus Status { get; set; } = LabItemStatus.Pending;
    public LabResult? Result { get; set; }
}

public class LabResult : BaseEntity
{
    public Guid LabOrderItemId { get; set; }
    public LabOrderItem LabOrderItem { get; set; } = null!;

    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public bool IsAbnormal { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? Notes { get; set; }
}
