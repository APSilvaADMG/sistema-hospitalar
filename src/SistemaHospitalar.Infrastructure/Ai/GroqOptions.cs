namespace SistemaHospitalar.Infrastructure.Ai;

public class GroqOptions
{
    public const string SectionName = "Groq";

    public bool Enabled { get; set; }

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "llama-3.3-70b-versatile";

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
}
