using System.Security.Cryptography;
using System.Text;

namespace SistemaHospitalar.Infrastructure.Connect;

public static class WhatsAppSignatureValidator
{
    public static bool IsValid(string rawBody, string? signatureHeader, string appSecret, bool useMockProvider)
    {
        if (string.IsNullOrWhiteSpace(appSecret))
        {
            // Em produção Meta, App Secret é obrigatório para validar X-Hub-Signature-256.
            return useMockProvider;
        }

        if (string.IsNullOrWhiteSpace(signatureHeader) || !signatureHeader.StartsWith("sha256=", StringComparison.Ordinal))
        {
            return false;
        }

        var expectedHex = signatureHeader["sha256=".Length..];
        var key = Encoding.UTF8.GetBytes(appSecret);
        var body = Encoding.UTF8.GetBytes(rawBody);
        var hash = HMACSHA256.HashData(key, body);
        var actualHex = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actualHex),
            Encoding.UTF8.GetBytes(expectedHex.ToLowerInvariant()));
    }
}
