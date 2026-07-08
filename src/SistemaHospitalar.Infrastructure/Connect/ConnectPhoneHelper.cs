namespace SistemaHospitalar.Infrastructure.Connect;

public static class ConnectPhoneHelper
{
    public static string Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("55") && digits.Length >= 12)
        {
            return digits;
        }

        if (digits.Length is 10 or 11)
        {
            return "55" + digits;
        }

        return digits;
    }

    public static string MaskCpf(string cpf)
    {
        var digits = new string(cpf.Where(char.IsDigit).ToArray());
        if (digits.Length != 11)
        {
            return cpf;
        }

        return $"***.{digits[3..6]}.{digits[6..9]}-**";
    }

    public static string DigitsOnly(string value)
        => new string(value.Where(char.IsDigit).ToArray());
}
