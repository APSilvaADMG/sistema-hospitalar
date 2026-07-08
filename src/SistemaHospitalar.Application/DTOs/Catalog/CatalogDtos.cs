namespace SistemaHospitalar.Application.DTOs.Catalog;

public record HealthInsuranceDto(
    Guid Id,
    string Name,
    string? AnsRegistration,
    string? Cnpj,
    string? LogoUrl,
    string? WebsiteUrl,
    bool IsActive);
public record SpecialtyDto(Guid Id, string Name, string? CboCode);
public record ProfessionalDto(Guid Id, string FullName, string? Crm, Guid SpecialtyId, string SpecialtyName, bool HasPhoto);
