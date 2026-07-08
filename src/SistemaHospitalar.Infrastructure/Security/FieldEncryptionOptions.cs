namespace SistemaHospitalar.Infrastructure.Security;

public class FieldEncryptionOptions
{
    public const string SectionName = "FieldEncryption";
    public string Key { get; set; } = string.Empty;
    public string HashKey { get; set; } = string.Empty;
}
