using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Infrastructure.OfficialUpdates;

namespace SistemaHospitalar.Infrastructure.Tiss;

public sealed class SigtapOfficialSyncService(
    IHttpClientFactory httpClientFactory,
    IOptions<OfficialUpdatesSettings> settings,
    ILogger<SigtapOfficialSyncService> logger) : ISigtapOfficialSyncService
{
    private static readonly Regex CompetenceTitleRegex = new(
        @"Compet[eê]ncia\s+(\d{2})/(\d{4})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly HashSet<string> AllowedDownloadHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "ftp.datasus.gov.br",
        "ftp2.datasus.gov.br",
    };

    public async Task<SigtapOfficialReleaseDto> DiscoverLatestAsync(CancellationToken cancellationToken = default)
    {
        var config = settings.Value.Sigtap;
        var rssUrl = config.RssFeedUrl
            ?? config.CheckUrl
            ?? "http://sigtap.datasus.gov.br/tabela-unificada/competencias.rss";

        logger.LogInformation("Consultando feed oficial SIGTAP em {RssUrl}", rssUrl);

        var client = httpClientFactory.CreateClient(nameof(SigtapOfficialSyncService));
        client.Timeout = TimeSpan.FromSeconds(60);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("APSMedCore-SigtapSync/1.0");

        using var response = await client.GetAsync(rssUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Falha ao consultar feed SIGTAP (HTTP {(int)response.StatusCode}).");

        var xml = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = XDocument.Parse(xml);
        var item = document.Descendants("item").FirstOrDefault()
            ?? throw new InvalidOperationException("Feed SIGTAP não contém competências disponíveis.");

        var title = item.Element("title")?.Value?.Trim()
            ?? throw new InvalidOperationException("Item SIGTAP sem título no feed oficial.");
        var downloadUrl = item.Element("link")?.Value?.Trim()
            ?? item.Element("guid")?.Value?.Trim()
            ?? throw new InvalidOperationException("Item SIGTAP sem URL de download no feed oficial.");

        var competence = ParseCompetenceFromTitle(title)
            ?? ParseCompetenceFromFileName(downloadUrl)
            ?? throw new InvalidOperationException($"Não foi possível identificar a competência em \"{title}\".");

        DateTime? publishedAt = null;
        if (DateTime.TryParse(item.Element("pubDate")?.Value, out var parsedPubDate))
            publishedAt = parsedPubDate.ToUniversalTime();

        EnsureAllowedDownloadUrl(downloadUrl);

        logger.LogInformation(
            "Competência SIGTAP mais recente: {Competence} ({Title}) — {DownloadUrl}",
            competence,
            title,
            downloadUrl);

        return new SigtapOfficialReleaseDto(competence, downloadUrl, title, publishedAt);
    }

    public async Task<SigtapOfficialDownloadDto> DownloadOfficialZipAsync(
        SigtapOfficialReleaseDto release,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(release);
        EnsureAllowedDownloadUrl(release.DownloadUrl);

        var maxBytes = settings.Value.MaxDownloadBytes;
        var minBytes = Math.Max(settings.Value.Sigtap.MinDownloadBytes, 50_000);

        logger.LogInformation(
            "Baixando tabela SIGTAP oficial ({Competence}) de {Url}",
            release.Competence,
            release.DownloadUrl);

        var data = release.DownloadUrl.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase)
            ? await DownloadFtpAsync(release.DownloadUrl, maxBytes, cancellationToken)
            : await DownloadHttpAsync(release.DownloadUrl, maxBytes, cancellationToken);

        ValidateZipPayload(data, minBytes, maxBytes);

        var fileName = Path.GetFileName(new Uri(release.DownloadUrl).LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = $"TabelaUnificada_{release.Competence.Replace("-", string.Empty, StringComparison.Ordinal)}.zip";

        var hash = ComputeSha256(data);

        logger.LogInformation(
            "Download SIGTAP concluído: {FileName}, {SizeBytes} bytes, SHA256 {Hash}",
            fileName,
            data.Length,
            hash);

        return new SigtapOfficialDownloadDto(data, fileName, release.DownloadUrl, hash, data.Length);
    }

    private async Task<byte[]> DownloadHttpAsync(string url, long maxBytes, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(SigtapOfficialSyncService));
        client.Timeout = TimeSpan.FromMinutes(10);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("APSMedCore-SigtapSync/1.0");

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Download SIGTAP falhou (HTTP {(int)response.StatusCode}).");

        return await ReadLimitedAsync(await response.Content.ReadAsStreamAsync(cancellationToken), maxBytes, cancellationToken);
    }

#pragma warning disable SYSLIB0014
    private static async Task<byte[]> DownloadFtpAsync(string url, long maxBytes, CancellationToken cancellationToken)
    {
        var request = (FtpWebRequest)WebRequest.Create(url);
        request.Method = WebRequestMethods.Ftp.DownloadFile;
        request.Credentials = new NetworkCredential("anonymous", "anonymous@datasus.gov.br");
        request.UseBinary = true;
        request.UsePassive = true;
        request.Timeout = (int)TimeSpan.FromMinutes(10).TotalMilliseconds;

        using var response = (FtpWebResponse)await request.GetResponseAsync();
        await using var stream = response.GetResponseStream()
            ?? throw new InvalidOperationException("Resposta FTP SIGTAP sem conteúdo.");

        return await ReadLimitedAsync(stream, maxBytes, cancellationToken);
    }
#pragma warning restore SYSLIB0014

    private static async Task<byte[]> ReadLimitedAsync(Stream stream, long maxBytes, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;
            if (total > maxBytes)
                throw new InvalidOperationException($"Arquivo SIGTAP excede o limite de {maxBytes / 1_048_576} MB.");

            memory.Write(buffer, 0, read);
        }

        return memory.ToArray();
    }

    private static void ValidateZipPayload(byte[] data, long minBytes, long maxBytes)
    {
        if (data.Length < minBytes)
            throw new InvalidOperationException($"Arquivo SIGTAP muito pequeno ({data.Length} bytes).");

        if (data.Length > maxBytes)
            throw new InvalidOperationException($"Arquivo SIGTAP excede o limite de {maxBytes / 1_048_576} MB.");

        if (data.Length < 4 || data[0] != 0x50 || data[1] != 0x4B)
            throw new InvalidOperationException("O arquivo baixado não parece ser um ZIP válido (assinatura PK ausente).");
    }

    private static void EnsureAllowedDownloadUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("URL de download SIGTAP inválida.");

        if (uri.Scheme.Equals("ftp", StringComparison.OrdinalIgnoreCase))
        {
            if (!AllowedDownloadHosts.Contains(uri.Host))
                throw new InvalidOperationException($"Host FTP não autorizado para SIGTAP: {uri.Host}");
            return;
        }

        if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            if (!uri.Host.EndsWith("datasus.gov.br", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Host HTTP não autorizado para SIGTAP: {uri.Host}");
            return;
        }

        throw new InvalidOperationException($"Protocolo não suportado para download SIGTAP: {uri.Scheme}");
    }

    private static string? ParseCompetenceFromTitle(string title)
    {
        var match = CompetenceTitleRegex.Match(title);
        if (!match.Success)
            return null;

        return $"{match.Groups[2].Value}-{match.Groups[1].Value}";
    }

    private static string? ParseCompetenceFromFileName(string fileName)
    {
        var match = Regex.Match(fileName, @"(20\d{2})(0[1-9]|1[0-2])", RegexOptions.CultureInvariant);
        return match.Success ? $"{match.Groups[1].Value}-{match.Groups[2].Value}" : null;
    }

    private static string ComputeSha256(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
