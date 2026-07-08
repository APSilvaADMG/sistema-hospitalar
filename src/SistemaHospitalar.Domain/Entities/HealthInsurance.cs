using SistemaHospitalar.Domain.Common;

namespace SistemaHospitalar.Domain.Entities;

public class HealthInsurance : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? AnsRegistration { get; set; }
    public string? Cnpj { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TissVersion { get; set; }
    public string? OperatorCode { get; set; }
    public string? PortalUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? WebServiceUrl { get; set; }
    public string? IntegrationUser { get; set; }
    public string? IntegrationSecret { get; set; }
    public bool UseMockIntegration { get; set; } = true;
    public int? AuthorizationDeadlineDays { get; set; }
    public bool RequiresOnlineAuthorization { get; set; }
    public bool RequiresEligibilityCheck { get; set; } = true;
    public string? BusinessRules { get; set; }

    public ICollection<PatientInsurance> PatientInsurances { get; set; } = [];
}
