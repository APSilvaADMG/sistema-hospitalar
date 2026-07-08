using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Infrastructure.Ai;

public class GroqLlmService(
    HttpClient httpClient,
    IOptions<GroqOptions> options,
    ILogger<GroqLlmService> logger) : IGroqLlmService
{
    private readonly GroqOptions _options = options.Value;

    public bool IsConfigured =>
        _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<string?> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return null;
        }

        var url = $"{_options.BaseUrl.TrimEnd('/')}/chat/completions";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new GroqChatRequest(
            _options.Model,
            [
                new GroqMessage("system", systemPrompt),
                new GroqMessage("user", userPrompt),
            ],
            0.3,
            1200,
            Stream: false));

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Groq API {Status}: {Body}", (int)response.StatusCode, body);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<GroqChatResponse>(cancellationToken);
            return payload?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao chamar Groq LLM");
            return null;
        }
    }

    public async IAsyncEnumerable<string> StreamCompleteAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            yield break;
        }

        var url = $"{_options.BaseUrl.TrimEnd('/')}/chat/completions";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new GroqChatRequest(
            _options.Model,
            [
                new GroqMessage("system", systemPrompt),
                new GroqMessage("user", userPrompt),
            ],
            0.3,
            1200,
            Stream: true));

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao iniciar stream Groq LLM");
            yield break;
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Groq stream API {Status}: {Body}", (int)response.StatusCode, body);
                yield break;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    yield break;
                }
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal))
                {
                    continue;
                }

                var data = line["data:".Length..].Trim();
                if (data == "[DONE]")
                {
                    yield break;
                }

                string? delta;
                try
                {
                    var chunk = JsonSerializer.Deserialize<GroqStreamChunk>(data);
                    delta = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
                }
                catch (JsonException)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(delta))
                {
                    yield return delta;
                }
            }
        }
    }

    private sealed record GroqChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] GroqMessage[] Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record GroqMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record GroqChatResponse(
        [property: JsonPropertyName("choices")] GroqChoice[]? Choices);

    private sealed record GroqChoice(
        [property: JsonPropertyName("message")] GroqMessage? Message);

    private sealed record GroqStreamChunk(
        [property: JsonPropertyName("choices")] GroqStreamChoice[]? Choices);

    private sealed record GroqStreamChoice(
        [property: JsonPropertyName("delta")] GroqStreamDelta? Delta);

    private sealed record GroqStreamDelta(
        [property: JsonPropertyName("content")] string? Content);
}
