using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SistemaHospitalar.Infrastructure.TvSignage;

public interface ITvSpeechService
{
    string Provider { get; }
    Task<byte[]?> SynthesizeAsync(string text, CancellationToken cancellationToken = default);
}

public class TvSpeechService(
    IHttpClientFactory httpClientFactory,
    IOptions<TvSignageSettings> options,
    ILogger<TvSpeechService> logger) : ITvSpeechService
{
    private readonly TvSpeechSettings _settings = options.Value.Speech;

    public string Provider => string.IsNullOrWhiteSpace(_settings.Provider) ? "Browser" : _settings.Provider;

    public async Task<byte[]?> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        return Provider.ToUpperInvariant() switch
        {
            "AZURE" => await SynthesizeAzureAsync(text, cancellationToken),
            "GOOGLE" => await SynthesizeGoogleAsync(text, cancellationToken),
            _ => null,
        };
    }

    private async Task<byte[]?> SynthesizeAzureAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.AzureKey) || string.IsNullOrWhiteSpace(_settings.AzureRegion))
        {
            return null;
        }

        try
        {
            var client = httpClientFactory.CreateClient("TvAzureSpeech");
            var ssml = $"""
                <speak version='1.0' xml:lang='pt-BR'>
                  <voice name='{_settings.AzureVoice}'>{System.Security.SecurityElement.Escape(text)}</voice>
                </speak>
                """;
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://{_settings.AzureRegion}.tts.speech.microsoft.com/cognitiveservices/v1");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _settings.AzureKey);
            request.Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/ssml+xml");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Azure Speech falhou: {Status}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Erro Azure Speech");
            return null;
        }
    }

    private async Task<byte[]?> SynthesizeGoogleAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.GoogleApiKey)) return null;

        try
        {
            var client = httpClientFactory.CreateClient("TvGoogleSpeech");
            var payload = new
            {
                input = new { text },
                voice = new { languageCode = "pt-BR", name = _settings.GoogleVoice },
                audioConfig = new { audioEncoding = "MP3" },
            };
            var url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={_settings.GoogleApiKey}";
            using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Google Speech falhou: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<GoogleTtsResponse>(cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(json?.AudioContent)) return null;
            return Convert.FromBase64String(json.AudioContent);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Erro Google Speech");
            return null;
        }
    }

    private sealed class GoogleTtsResponse
    {
        public string? AudioContent { get; set; }
    }
}
