using System.Globalization;
using System.Text;

namespace SistemaHospitalar.Infrastructure.Pix;

public static class PixBrCodeHelper
{
    public static string GenerateTxId()
        => Guid.NewGuid().ToString("N")[..25].ToUpperInvariant();

    public static string BuildCopyPasteCode(
        string pixKey,
        string merchantName,
        string city,
        decimal amount,
        string txId)
    {
        var normalizedName = Sanitize(merchantName, 25);
        var normalizedCity = Sanitize(city, 15);
        var amountText = amount.ToString("0.00", CultureInfo.InvariantCulture);

        var merchantAccount = Join(
            Field("00", "br.gov.bcb.pix"),
            Field("01", pixKey));

        var additionalData = Field("05", txId);

        var payload = Join(
            Field("00", "01"),
            Field("01", "12"),
            Field("26", merchantAccount),
            Field("52", "0000"),
            Field("53", "986"),
            Field("54", amountText),
            Field("58", "BR"),
            Field("59", normalizedName),
            Field("60", normalizedCity),
            Field("62", additionalData),
            "6304");

        var crc = Crc16(payload);
        return payload + crc;
    }

    private static string Field(string id, string value)
        => $"{id}{value.Length:D2}{value}";

    private static string Join(params string[] parts)
        => string.Concat(parts);

    private static string Sanitize(string value, int maxLength)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (char.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch) || ch is ' ' or '.' or '-')
            {
                builder.Append(char.ToUpperInvariant(ch));
            }
        }

        var text = builder.ToString().Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static string Crc16(string payload)
    {
        const ushort polynomial = 0x1021;
        ushort crc = 0xFFFF;

        foreach (var b in Encoding.UTF8.GetBytes(payload))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0
                    ? (ushort)((crc << 1) ^ polynomial)
                    : (ushort)(crc << 1);
            }
        }

        return (crc & 0xFFFF).ToString("X4");
    }
}
