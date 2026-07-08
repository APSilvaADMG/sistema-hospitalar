using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class Patient : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? SocialName { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public string? CpfHash { get; set; }
    public string? CnsHash { get; set; }
    public string? Cns { get; set; }
    public DateOnly BirthDate { get; set; }
    public Gender Gender { get; set; } = Gender.NotInformed;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? AddressStreet { get; set; }
    public string? AddressNumber { get; set; }
    public string? AddressComplement { get; set; }
    public string? AddressNeighborhood { get; set; }
    public string? AddressCity { get; set; }
    public string? AddressState { get; set; }
    public string? AddressZipCode { get; set; }
    public string? MotherName { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? Notes { get; set; }
    public string? PhotoData { get; set; }
    public string? Rg { get; set; }
    public string? Nationality { get; set; }
    public string? BloodType { get; set; }
    public string? Occupation { get; set; }
    public string? MaritalStatus { get; set; }
    public string? BirthPlace { get; set; }
    public bool IsDeceased { get; set; }
    public DateTime? DeceasedAt { get; set; }
    public bool UsesResponsibleCpf { get; set; }
    public string? LegalResponsibleName { get; set; }
    public DateOnly? LegalResponsibleBirthDate { get; set; }
    public LegalResponsibleRelationship? LegalResponsibleRelationship { get; set; }
    public string? LegalResponsibleRg { get; set; }
    public LegalAuthorizationDocumentType? LegalAuthorizationDocumentType { get; set; }
    public string? LegalAuthorizationDocumentReference { get; set; }

    public ICollection<PatientInsurance> Insurances { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
    public MedicalRecord? MedicalRecord { get; set; }
    public ICollection<FinancialAccount> FinancialAccounts { get; set; } = [];
}
