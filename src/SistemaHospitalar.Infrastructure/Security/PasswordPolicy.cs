using System.Text.RegularExpressions;

namespace SistemaHospitalar.Infrastructure.Security;

public static partial class PasswordPolicy
{
    public const int MinLength = 10;
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public static void ValidateOrThrow(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
        {
            throw new InvalidOperationException($"A senha deve ter no mínimo {MinLength} caracteres.");
        }

        if (!UpperCaseRegex().IsMatch(password))
        {
            throw new InvalidOperationException("A senha deve conter ao menos uma letra maiúscula.");
        }

        if (!LowerCaseRegex().IsMatch(password))
        {
            throw new InvalidOperationException("A senha deve conter ao menos uma letra minúscula.");
        }

        if (!DigitRegex().IsMatch(password))
        {
            throw new InvalidOperationException("A senha deve conter ao menos um número.");
        }

        if (!SymbolRegex().IsMatch(password))
        {
            throw new InvalidOperationException("A senha deve conter ao menos um caractere especial.");
        }
    }

    [GeneratedRegex("[A-Z]")]
    private static partial Regex UpperCaseRegex();

    [GeneratedRegex("[a-z]")]
    private static partial Regex LowerCaseRegex();

    [GeneratedRegex("[0-9]")]
    private static partial Regex DigitRegex();

    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex SymbolRegex();
}
