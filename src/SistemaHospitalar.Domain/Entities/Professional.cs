using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class Professional : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? SocialName { get; set; }
    public string? Crm { get; set; }
    public string? CouncilUf { get; set; }
    public string? Cpf { get; set; }
    public string? Rg { get; set; }
    public DateOnly? BirthDate { get; set; }
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
    public string? Notes { get; set; }
    public string? PhotoData { get; set; }

    public Guid SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;

    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<MedicalRecordEntry> MedicalRecordEntries { get; set; } = [];
}
