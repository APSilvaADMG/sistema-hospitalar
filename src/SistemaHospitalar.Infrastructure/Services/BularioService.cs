using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Infrastructure.Services;

public class BularioService(
    HttpClient httpClient,
    ILogger<BularioService> logger) : IBularioService
{
    private const string AnvisaApiBase = "https://consultas.anvisa.gov.br/api/";
    private static readonly TimeSpan ProbeCacheDuration = TimeSpan.FromMinutes(10);
    private static bool? _anvisaAvailableCache;
    private static DateTime _anvisaProbeAt = DateTime.MinValue;

    public async Task<bool> IsAnvisaAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (_anvisaAvailableCache.HasValue && DateTime.UtcNow - _anvisaProbeAt < ProbeCacheDuration)
            return _anvisaAvailableCache.Value;

        try
        {
            using var response = await httpClient.GetAsync(
                $"{AnvisaApiBase}consulta/bulario?count=1&filter%5BnomeProduto%5D=dipirona&page=1",
                cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            var available = response.IsSuccessStatusCode && contentType.Contains("json", StringComparison.OrdinalIgnoreCase);
            _anvisaAvailableCache = available;
            _anvisaProbeAt = DateTime.UtcNow;
            if (!available)
                logger.LogInformation("ANVISA indisponível (HTTP {Status}, content-type {Type})", response.StatusCode, contentType);
            return available;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao verificar disponibilidade da ANVISA");
            _anvisaAvailableCache = false;
            _anvisaProbeAt = DateTime.UtcNow;
            return false;
        }
    }

    public async Task<JsonDocument?> SearchAsync(string name, int page = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (!await IsAnvisaAvailableAsync(cancellationToken))
            return null;

        var url =
            $"{AnvisaApiBase}consulta/bulario?count=10&filter%5BnomeProduto%5D={Uri.EscapeDataString(name.Trim())}&page={page}";
        return await GetAnvisaJsonAsync(url, cancellationToken);
    }

    public async Task<JsonDocument?> GetMedicationAsync(string processNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(processNumber))
            return null;

        var url = $"{AnvisaApiBase}consulta/medicamento/produtos/{Uri.EscapeDataString(processNumber)}";
        return await GetAnvisaJsonAsync(url, cancellationToken);
    }

    public Task<JsonDocument?> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => GetAnvisaJsonAsync($"{AnvisaApiBase}tipoCategoriaRegulatoria", cancellationToken);

    public async Task<byte[]?> GetPdfAsync(string bulaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bulaId))
            return null;

        try
        {
            var url =
                $"{AnvisaApiBase}consulta/medicamentos/arquivo/bula/parecer/{Uri.EscapeDataString(bulaId)}/?Authorization=";
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("PDF ANVISA {Id} retornou {Status}", bulaId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao obter PDF ANVISA {Id}", bulaId);
            return null;
        }
    }

    public Task<string?> GetPdfUrlAsync(string bulaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bulaId))
            return Task.FromResult<string?>(null);

        var url =
            $"{AnvisaApiBase}consulta/medicamentos/arquivo/bula/parecer/{Uri.EscapeDataString(bulaId)}/?Authorization=";
        return Task.FromResult<string?>(url);
    }

    private async Task<JsonDocument?> GetAnvisaJsonAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode || body.TrimStart().StartsWith('<'))
            {
                logger.LogWarning("ANVISA {Url} retornou {Status}", url, response.StatusCode);
                return null;
            }

            return JsonDocument.Parse(body);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha na consulta ANVISA: {Url}", url);
            return null;
        }
    }
}

public static class BularioServiceExtensions
{
    public static IServiceCollection AddBularioIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration;
        services.AddHttpClient<IBularioService, BularioService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(45);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Guest");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://consultas.anvisa.gov.br/");
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            });

        return services;
    }
}
