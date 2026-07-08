using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class FinancialAccountLineItem : BaseEntity
{
    public Guid FinancialAccountId { get; set; }
    public FinancialAccount FinancialAccount { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}

public class FinancialCashSession : BaseEntity
{
    public string Label { get; set; } = "Caixa principal";
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public decimal ExpectedBalance { get; set; }
    public string? Notes { get; set; }
    public Guid? OpenedByUserId { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public FinancialCashSessionStatus Status { get; set; } = FinancialCashSessionStatus.Open;
}

public class WardStockBalance : BaseEntity
{
    public Guid WardId { get; set; }
    public Ward Ward { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal QuantityOnHand { get; set; }
    public decimal MinimumStock { get; set; }
    public string Unit { get; set; } = "UN";
}

public class WardStockMovement : BaseEntity
{
    public Guid WardId { get; set; }
    public Ward Ward { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public WardStockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "UN";

    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime MovementDate { get; set; }
}

public class VaccineCatalog : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public VaccineScheduleType ScheduleType { get; set; } = VaccineScheduleType.Child;
    public int DisplayOrder { get; set; }
}

public class PatientVaccination : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid VaccineCatalogId { get; set; }
    public VaccineCatalog VaccineCatalog { get; set; } = null!;

    public DateTime AdministeredAt { get; set; }
    public int DoseNumber { get; set; } = 1;
    public string? BatchNumber { get; set; }
    public Guid? ProfessionalId { get; set; }
    public Professional? Professional { get; set; }
    public string? Notes { get; set; }
}

public class EpidemicDiseaseCatalog : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EpidemicDiseaseClass DiseaseClass { get; set; }
    public bool IncludeOpd { get; set; } = true;
    public bool IncludeIpd { get; set; } = true;
    public int DisplayOrder { get; set; }
}

/// <summary>Eventos de leito (reserva, bloqueio, ocupação, liberação) — inspirado no Madre.</summary>
public class BedEvent : BaseEntity
{
    public Guid BedId { get; set; }
    public Bed Bed { get; set; } = null!;

    public BedEventType EventType { get; set; }
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public string? Reason { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }
}

/// <summary>Estorno de dispensação farmacêutica — inspirado no Madre.</summary>
public class PharmacyDispensingReversal : BaseEntity
{
    public Guid DispensingId { get; set; }
    public PharmacyDispensing Dispensing { get; set; } = null!;

    public decimal Quantity { get; set; }
    public string? Reason { get; set; }
    public Guid? ReversedByUserId { get; set; }
    public DateTime ReversedAt { get; set; }
}

/// <summary>Vias de administração de medicamentos (ANVISA/SUS).</summary>
public class AdministrationRouteCatalog : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Abbreviation { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>Catálogos de referência do cadastro de pacientes (raça, etnia, etc.).</summary>
public class PatientReferenceCatalogItem : BaseEntity
{
    public PatientReferenceCatalogType CatalogType { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
