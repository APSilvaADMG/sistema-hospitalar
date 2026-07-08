namespace SistemaHospitalar.Infrastructure.Services;

internal static class FieldNormalizers
{
    public static string NormalizeDigits(string value) =>
        new string(value.Where(char.IsDigit).ToArray());

    public static string? NormalizeZipCode(string? zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
        {
            return null;
        }

        var digits = NormalizeDigits(zipCode);
        return digits.Length > 0 ? digits : null;
    }
}
