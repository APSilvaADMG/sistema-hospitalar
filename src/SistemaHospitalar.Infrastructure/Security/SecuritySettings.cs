namespace SistemaHospitalar.Infrastructure.Security;

public class SecuritySettings
{
    public const string SectionName = "Security";

    /// <summary>Quando true, o cadastro de visitante exige foto.</summary>
    public bool VisitorPhotoRequired { get; set; }
}
