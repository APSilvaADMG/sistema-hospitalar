namespace SistemaHospitalar.Domain.BusinessRules;

public static class PatientCpfRules
{
    public const string PlaceholderCpf = "00000000000";

    public static string Normalize(string? cpf) =>
        new string((cpf ?? string.Empty).Where(char.IsDigit).ToArray());

    public static bool IsMissing(string? cpf)
    {
        var normalized = Normalize(cpf);
        return normalized.Length == 0 || normalized == PlaceholderCpf;
    }

    public static void ValidateFormat(string normalizedCpf)
    {
        if (normalizedCpf.Length != 11)
        {
            throw new InvalidOperationException("CPF deve conter 11 dígitos.");
        }

        if (!IsValidChecksum(normalizedCpf))
        {
            throw new InvalidOperationException("CPF inválido.");
        }
    }

    public static void ValidateForRegistration(string? cpf, bool cpfAlreadyRegistered)
    {
        if (IsMissing(cpf))
        {
            return;
        }

        var normalized = Normalize(cpf);
        ValidateFormat(normalized);
        HospitalBusinessRules.ValidateUniqueCpf(cpfAlreadyRegistered);
    }

    public static bool IsValidChecksum(string cpf)
    {
        if (cpf.Length != 11)
        {
            return false;
        }

        if (cpf.Distinct().Count() == 1)
        {
            return false;
        }

        var digits = cpf.Select(c => c - '0').ToArray();

        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            sum += digits[i] * (10 - i);
        }

        var remainder = sum % 11;
        var firstDigit = remainder < 2 ? 0 : 11 - remainder;
        if (digits[9] != firstDigit)
        {
            return false;
        }

        sum = 0;
        for (var i = 0; i < 10; i++)
        {
            sum += digits[i] * (11 - i);
        }

        remainder = sum % 11;
        var secondDigit = remainder < 2 ? 0 : 11 - remainder;
        return digits[10] == secondDigit;
    }
}
