namespace SistemaHospitalar.Infrastructure.TvSignage;

public class TvSignageSettings
{
    public const string SectionName = "TvSignage";

    public string PlayerBaseUrl { get; set; } = "http://localhost:5173";

    public TvSpeechSettings Speech { get; set; } = new();

    public TvWeatherSettings Weather { get; set; } = new();
}

public class TvSpeechSettings
{
    /// <summary>Azure, Google ou Browser (player usa Web Speech API).</summary>
    public string Provider { get; set; } = "Browser";

    public string AzureKey { get; set; } = string.Empty;
    public string AzureRegion { get; set; } = "brazilsouth";
    public string AzureVoice { get; set; } = "pt-BR-FranciscaNeural";

    public string GoogleApiKey { get; set; } = string.Empty;
    public string GoogleVoice { get; set; } = "pt-BR-Neural2-A";
}

public class TvWeatherSettings
{
    public string OpenWeatherApiKey { get; set; } = string.Empty;
    public string DefaultCity { get; set; } = "Arapiraca";
    public string DefaultCountryCode { get; set; } = "BR";
    public int RefreshMinutes { get; set; } = 60;
}
