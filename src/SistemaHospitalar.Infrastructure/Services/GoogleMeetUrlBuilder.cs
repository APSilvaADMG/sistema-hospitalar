namespace SistemaHospitalar.Infrastructure.Services;

internal static class GoogleMeetUrlBuilder
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz";

    public static string FromAppointmentId(Guid appointmentId)
    {
        var bytes = appointmentId.ToByteArray();
        Span<char> code = stackalloc char[10];

        for (var i = 0; i < code.Length; i++)
        {
            code[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return $"https://meet.google.com/{code[..3]}-{code[3..7]}-{code[7..10]}";
    }

    public static bool IsValidMeetUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Host.Equals("meet.google.com", StringComparison.OrdinalIgnoreCase);
    }

    public static string Resolve(Guid appointmentId, string? storedUrl)
    {
        return IsValidMeetUrl(storedUrl) ? storedUrl! : FromAppointmentId(appointmentId);
    }
}
