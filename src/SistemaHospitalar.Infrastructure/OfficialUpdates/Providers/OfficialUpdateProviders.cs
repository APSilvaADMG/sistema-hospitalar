using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Tiss;

namespace SistemaHospitalar.Infrastructure.OfficialUpdates.Providers;

public abstract class OfficialUpdateProviderBase(
    AppDbContext dbContext,
    IOptions<OfficialUpdatesSettings> settings,
    IHttpClientFactory httpClientFactory,
    ILogger logger)
{
    protected AppDbContext Db => dbContext;
    protected OfficialUpdatesSettings Settings => settings.Value;
    protected ILogger Logger => logger;

    protected OfficialSourceSettings SourceConfig => SourceType switch
    {
        OfficialSourceType.Tuss => Settings.Tuss,
        OfficialSourceType.Tiss => Settings.Tiss,
        OfficialSourceType.Sigtap => Settings.Sigtap,
        OfficialSourceType.Ans => Settings.Ans,
        OfficialSourceType.SusTables => Settings.SusTables,
        OfficialSourceType.Anvisa => Settings.Anvisa,
        OfficialSourceType.Brasindice => Settings.Brasindice,
        OfficialSourceType.Simpro => Settings.Simpro,
        _ => throw new ArgumentOutOfRangeException(),
    };

    public abstract OfficialSourceType SourceType { get; }

    public abstract Task<OfficialCheckResult> CheckAsync(CancellationToken cancellationToken);

    public abstract Task<OfficialImportResult> ImportAsync(CancellationToken cancellationToken);

    protected async Task<string?> ProbeRemoteVersionAsync(string? url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        try
        {
            var client = httpClientFactory.CreateClient(nameof(OfficialUpdatesService));
            client.Timeout = TimeSpan.FromSeconds(30);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("APSMedCore-OfficialUpdates/1.0");
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var lastModified = response.Content.Headers.LastModified?.UtcDateTime;
            if (lastModified.HasValue)
                return lastModified.Value.ToString("yyyyMM");

            var etag = response.Headers.ETag?.Tag?.Trim('"');
            if (!string.IsNullOrWhiteSpace(etag) && etag.Length <= 64)
                return etag;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
            {
                var html = await response.Content.ReadAsStringAsync(cancellationToken);
                var match = Regex.Match(
                    html,
                    @"(?:vers[aã]o|compet[eê]ncia|atualiza[cç][aã]o)[^\d]{0,20}(\d{6}|\d{4}\.\d{2})",
                    RegexOptions.IgnoreCase);
                if (match.Success)
                    return match.Groups[1].Value;
            }

            return response.Headers.Date?.UtcDateTime.ToString("yyyyMMdd")
                   ?? DateTime.UtcNow.ToString("yyyyMM");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Falha ao sondar versão remota de {Source}", SourceType);
            return null;
        }
    }

    protected static string ComputeSha256(string text)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    protected static string ComputeSha256(Stream stream)
    {
        stream.Position = 0;
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    protected static string ComputeSha256(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    protected async Task<(byte[]? Data, string? Hash, string? Error)> TryDownloadAsync(
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient(nameof(OfficialUpdatesService));
            client.Timeout = TimeSpan.FromMinutes(5);
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return (null, null, $"HTTP {(int)response.StatusCode}");

            var length = response.Content.Headers.ContentLength;
            if (length > Settings.MaxDownloadBytes)
                return (null, null, $"Arquivo excede limite de {Settings.MaxDownloadBytes / 1_048_576} MB");

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var ms = new MemoryStream();
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                total += read;
                if (total > Settings.MaxDownloadBytes)
                    return (null, null, $"Download interrompido — limite de {Settings.MaxDownloadBytes / 1_048_576} MB");

                ms.Write(buffer, 0, read);
            }

            var data = ms.ToArray();
            return (data, ComputeSha256(data), null);
        }
        catch (Exception ex)
        {
            return (null, null, ex.Message);
        }
    }

    protected async Task<int> CountRecordsAsync(CancellationToken cancellationToken) =>
        SourceType switch
        {
            OfficialSourceType.Tuss => await Db.TussCatalogs.CountAsync(cancellationToken),
            OfficialSourceType.Sigtap => await Db.SigtapProcedures.CountAsync(cancellationToken),
            OfficialSourceType.Brasindice => await Db.BrasindiceItems.CountAsync(cancellationToken),
            OfficialSourceType.Simpro => await Db.SimproItems.CountAsync(cancellationToken),
            _ => 0,
        };
}

public sealed class TussOfficialUpdateProvider(
    AppDbContext dbContext,
    ITissExtendedService tissExtendedService,
    IOptions<OfficialUpdatesSettings> settings,
    IHttpClientFactory httpClientFactory,
    ILogger<TussOfficialUpdateProvider> logger)
    : OfficialUpdateProviderBase(dbContext, settings, httpClientFactory, logger), IOfficialUpdateProvider
{
    public override OfficialSourceType SourceType => OfficialSourceType.Tuss;

    public override async Task<OfficialCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var config = SourceConfig;
        var recordCount = await CountRecordsAsync(cancellationToken);
        var bundledFolder = TissBundledCatalogLocator.ResolveFolder202601();
        var bundledVersion = config.BundledFolderVersion ?? "202601";
        var currentVersion = recordCount > 0 ? bundledVersion : "—";

        string? folderHash = null;
        if (bundledFolder is not null)
        {
            var files = TissBundledCatalogLocator.FindTussXlsxFiles(bundledFolder);
            if (files.Count > 0)
            {
                var newest = files
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .First();
                folderHash = ComputeSha256($"{newest.FullName}|{newest.Length}|{newest.LastWriteTimeUtc:O}");
                currentVersion = bundledVersion;
            }
        }

        var remoteVersion = config.ExpectedVersion
            ?? await ProbeRemoteVersionAsync(config.CheckUrl ?? config.SourceUrl, cancellationToken)
            ?? bundledVersion;

        var status = recordCount == 0
            ? OfficialVersionStatus.UpdateAvailable
            : folderHash is not null && remoteVersion != bundledVersion
                ? OfficialVersionStatus.UpdateAvailable
                : OfficialVersionStatus.UpToDate;

        if (bundledFolder is null && recordCount == 0)
        {
            status = OfficialVersionStatus.ManualDownloadRequired;
        }

        return new OfficialCheckResult(
            currentVersion,
            remoteVersion,
            folderHash,
            config.ExpectedFileHash,
            status,
            recordCount,
            bundledFolder is null
                ? "Pasta Diversos/TISS/202601 não encontrada — coloque os XLSX ANS ou use importação manual."
                : $"{TissBundledCatalogLocator.FindTussXlsxFiles(bundledFolder).Count} arquivo(s) XLSX no pacote local.",
            bundledFolder is not null && config.AutoImportBundled);
    }

    public override async Task<OfficialImportResult> ImportAsync(CancellationToken cancellationToken)
    {
        if (!SourceConfig.AutoImportBundled)
        {
            return new OfficialImportResult(
                false,
                "Importação automática TUSS desabilitada na configuração.",
                null,
                null,
                null);
        }

        var bundledFolder = TissBundledCatalogLocator.ResolveFolder202601();
        if (bundledFolder is null)
        {
            return new OfficialImportResult(
                false,
                "Pacote TUSS local não encontrado (Diversos/TISS/202601). Baixe os arquivos ANS e coloque na pasta.",
                null,
                null,
                null);
        }

        var sw = Stopwatch.StartNew();
        var result = await tissExtendedService.ImportBundledTuss202601Async(cancellationToken);
        sw.Stop();

        var files = TissBundledCatalogLocator.FindTussXlsxFiles(bundledFolder);
        string? hash = null;
        if (files.Count > 0)
        {
            var newest = files.Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTimeUtc).First();
            hash = ComputeSha256($"{newest.FullName}|{newest.Length}|{newest.LastWriteTimeUtc:O}");
        }

        return new OfficialImportResult(
            result.Imported > 0 || result.TotalInFile > 0,
            result.Message,
            result.Imported,
            SourceConfig.BundledFolderVersion ?? "202601",
            hash);
    }
}

public sealed class TissOfficialUpdateProvider(
    AppDbContext dbContext,
    IOptions<OfficialUpdatesSettings> settings,
    IHttpClientFactory httpClientFactory,
    ILogger<TissOfficialUpdateProvider> logger)
    : OfficialUpdateProviderBase(dbContext, settings, httpClientFactory, logger), IOfficialUpdateProvider
{
    public override OfficialSourceType SourceType => OfficialSourceType.Tiss;

    public override async Task<OfficialCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var config = SourceConfig;
        var current = config.ExpectedVersion ?? "3.05.00";
        var remote = await ProbeRemoteVersionAsync(config.CheckUrl ?? config.SourceUrl, cancellationToken);

        var status = config.ManualDownloadRequired
            ? OfficialVersionStatus.ManualDownloadRequired
            : remote is not null && remote != current
                ? OfficialVersionStatus.UpdateAvailable
                : OfficialVersionStatus.UpToDate;

        return new OfficialCheckResult(
            current,
            remote,
            null,
            config.ExpectedFileHash,
            status,
            null,
            "Layouts XML TISS e anexos devem ser obtidos no portal ANS. Importação de guias via módulo TISS.",
            false);
    }

    public override Task<OfficialImportResult> ImportAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new OfficialImportResult(
            false,
            "TISS: atualize layouts XML e anexos manualmente no portal ANS. Use o módulo TISS para guias e lotes.",
            null,
            null,
            null));
}

public sealed class SigtapOfficialUpdateProvider(
    AppDbContext dbContext,
    ITissExtendedService tissExtendedService,
    ISigtapOfficialSyncService sigtapOfficialSyncService,
    IOptions<OfficialUpdatesSettings> settings,
    IHttpClientFactory httpClientFactory,
    ILogger<SigtapOfficialUpdateProvider> logger)
    : OfficialUpdateProviderBase(dbContext, settings, httpClientFactory, logger), IOfficialUpdateProvider
{
    public override OfficialSourceType SourceType => OfficialSourceType.Sigtap;

    public override async Task<OfficialCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var config = SourceConfig;
        var recordCount = await CountRecordsAsync(cancellationToken);
        var summary = await tissExtendedService.GetSigtapSummaryAsync(cancellationToken);
        var current = summary.LastCompetence ?? (recordCount > 0 ? $"db-{recordCount}" : "—");

        string? remote = config.ExpectedVersion;
        OfficialVersionStatus status;
        string notes;

        try
        {
            var release = await sigtapOfficialSyncService.DiscoverLatestAsync(cancellationToken);
            remote = release.Competence;

            status = recordCount == 0
                ? OfficialVersionStatus.UpdateAvailable
                : !string.Equals(remote, current, StringComparison.Ordinal)
                    ? OfficialVersionStatus.UpdateAvailable
                    : OfficialVersionStatus.UpToDate;
            notes = config.AutoSyncOfficial
                ? $"SIGTAP: competência remota {remote} via feed oficial DATASUS."
                : "SIGTAP: sincronização automática desabilitada — use upload manual.";
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Falha ao verificar competência remota SIGTAP");
            status = recordCount == 0
                ? OfficialVersionStatus.ManualDownloadRequired
                : OfficialVersionStatus.UpToDate;
            notes = "Não foi possível consultar o feed oficial SIGTAP. Tente novamente ou use upload manual.";
            remote = null;
        }

        return new OfficialCheckResult(
            current,
            remote,
            null,
            config.ExpectedFileHash,
            status,
            recordCount,
            notes,
            config.AutoSyncOfficial);
    }

    public override async Task<OfficialImportResult> ImportAsync(CancellationToken cancellationToken)
    {
        if (!SourceConfig.AutoSyncOfficial)
        {
            return new OfficialImportResult(
                false,
                "SIGTAP: sincronização automática desabilitada na configuração. Use upload manual na aba SIGTAP/SUS.",
                null,
                null,
                null);
        }

        try
        {
            var result = await tissExtendedService.SyncSigtapOfficialAsync(cancellationToken);
            return new OfficialImportResult(
                result.Success,
                result.Message,
                result.Inserted + result.Updated,
                result.RemoteCompetence,
                result.FileHash);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Importação automática SIGTAP falhou");
            return new OfficialImportResult(false, ex.Message, null, null, null);
        }
    }
}

public sealed class AnsOfficialUpdateProvider(
    AppDbContext dbContext,
    IOptions<OfficialUpdatesSettings> settings,
    IHttpClientFactory httpClientFactory,
    ILogger<AnsOfficialUpdateProvider> logger)
    : OfficialUpdateProviderBase(dbContext, settings, httpClientFactory, logger), IOfficialUpdateProvider
{
    public override OfficialSourceType SourceType => OfficialSourceType.Ans;

    public override async Task<OfficialCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var config = SourceConfig;
        var remote = await ProbeRemoteVersionAsync(config.CheckUrl ?? config.SourceUrl, cancellationToken);

        return new OfficialCheckResult(
            config.ExpectedVersion ?? "portal-ans",
            remote,
            null,
            config.ExpectedFileHash,
            OfficialVersionStatus.ManualDownloadRequired,
            null,
            "Normativas ANS (RN, resoluções, anexos TUSS/TISS) exigem acompanhamento manual no portal gov.br/ans.",
            false);
    }

    public override Task<OfficialImportResult> ImportAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new OfficialImportResult(
            false,
            "ANS: consulte o portal de normativas. Nenhuma importação automática configurada.",
            null,
            null,
            null));
}

public sealed class SusTablesOfficialUpdateProvider(
    AppDbContext dbContext,
    IOptions<OfficialUpdatesSettings> settings,
    IHttpClientFactory httpClientFactory,
    ILogger<SusTablesOfficialUpdateProvider> logger)
    : OfficialUpdateProviderBase(dbContext, settings, httpClientFactory, logger), IOfficialUpdateProvider
{
    public override OfficialSourceType SourceType => OfficialSourceType.SusTables;

    public override async Task<OfficialCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var config = SourceConfig;
        var remote = await ProbeRemoteVersionAsync(config.CheckUrl ?? config.SourceUrl, cancellationToken);

        return new OfficialCheckResult(
            config.ExpectedVersion ?? "datasus-transferencia",
            remote,
            null,
            config.ExpectedFileHash,
            OfficialVersionStatus.ManualDownloadRequired,
            null,
            "Tabelas SUS (CID, procedimentos, medicamentos) via DATASUS — download manual com credenciais quando exigido.",
            false);
    }

    public override Task<OfficialImportResult> ImportAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new OfficialImportResult(
            false,
            "Tabelas SUS: utilize o portal de transferência DATASUS. Pipeline de importação automática não disponível no MVP.",
            null,
            null,
            null));
}

public sealed class AnvisaOfficialUpdateProvider(
    AppDbContext dbContext,
    IOptions<OfficialUpdatesSettings> settings,
    IHttpClientFactory httpClientFactory,
    ILogger<AnvisaOfficialUpdateProvider> logger)
    : OfficialUpdateProviderBase(dbContext, settings, httpClientFactory, logger), IOfficialUpdateProvider
{
    public override OfficialSourceType SourceType => OfficialSourceType.Anvisa;

    public override async Task<OfficialCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var config = SourceConfig;
        var medicationCount = await Db.MedicationCatalogs.CountAsync(cancellationToken);
        var remote = await ProbeRemoteVersionAsync(config.CheckUrl ?? config.SourceUrl, cancellationToken);

        return new OfficialCheckResult(
            medicationCount > 0 ? $"catalog-{medicationCount}" : "—",
            remote,
            null,
            config.ExpectedFileHash,
            OfficialVersionStatus.ManualDownloadRequired,
            medicationCount,
            "ANVISA: bulas oficiais via Consulta ANVISA e importação JSONL (Consulta Remédios). Atualização incremental pelo módulo de bulas.",
            false);
    }

    public override Task<OfficialImportResult> ImportAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new OfficialImportResult(
            false,
            "ANVISA: utilize o importador de bulas (Consulta Remédios JSONL) ou consulta em tempo real no módulo Bulário.",
            null,
            null,
            null));
}

public sealed class BrasindiceOfficialUpdateProvider(
    AppDbContext dbContext,
    IOptions<OfficialUpdatesSettings> settings,
    IHttpClientFactory httpClientFactory,
    ILogger<BrasindiceOfficialUpdateProvider> logger)
    : OfficialUpdateProviderBase(dbContext, settings, httpClientFactory, logger), IOfficialUpdateProvider
{
    public override OfficialSourceType SourceType => OfficialSourceType.Brasindice;

    public override async Task<OfficialCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var config = SourceConfig;
        var recordCount = await CountRecordsAsync(cancellationToken);
        var remote = await ProbeRemoteVersionAsync(config.CheckUrl ?? config.SourceUrl, cancellationToken);

        return new OfficialCheckResult(
            recordCount > 0 ? $"db-{recordCount}" : "—",
            remote,
            null,
            config.ExpectedFileHash,
            OfficialVersionStatus.ManualDownloadRequired,
            recordCount,
            "Brasíndice: tabela comercial de preços de medicamentos — assinatura comercial necessária para atualização.",
            false);
    }

    public override Task<OfficialImportResult> ImportAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new OfficialImportResult(
            false,
            "Brasíndice: importação manual via arquivo licenciado. Catálogo demo já incluído no seed inicial.",
            null,
            null,
            null));
}

public sealed class SimproOfficialUpdateProvider(
    AppDbContext dbContext,
    IOptions<OfficialUpdatesSettings> settings,
    IHttpClientFactory httpClientFactory,
    ILogger<SimproOfficialUpdateProvider> logger)
    : OfficialUpdateProviderBase(dbContext, settings, httpClientFactory, logger), IOfficialUpdateProvider
{
    public override OfficialSourceType SourceType => OfficialSourceType.Simpro;

    public override async Task<OfficialCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var config = SourceConfig;
        var recordCount = await CountRecordsAsync(cancellationToken);
        var remote = await ProbeRemoteVersionAsync(config.CheckUrl ?? config.SourceUrl, cancellationToken);

        return new OfficialCheckResult(
            recordCount > 0 ? $"db-{recordCount}" : "—",
            remote,
            null,
            config.ExpectedFileHash,
            OfficialVersionStatus.ManualDownloadRequired,
            recordCount,
            "SIMPRO: tabela de materiais, OPME e medicamentos — assinatura comercial necessária para atualização.",
            false);
    }

    public override Task<OfficialImportResult> ImportAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new OfficialImportResult(
            false,
            "SIMPRO: importação manual via arquivo licenciado. Catálogo demo já incluído no seed inicial.",
            null,
            null,
            null));
}
