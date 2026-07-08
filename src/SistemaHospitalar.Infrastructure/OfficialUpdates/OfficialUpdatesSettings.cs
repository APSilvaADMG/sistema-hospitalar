namespace SistemaHospitalar.Infrastructure.OfficialUpdates;

public class OfficialUpdatesSettings
{
    public const string SectionName = "OfficialUpdates";

    public bool Enabled { get; set; } = true;

    /// <summary>Horário diário UTC para verificação automática (formato HH:mm).</summary>
    public string DailyRunTimeUtc { get; set; } = "02:00";

    /// <summary>Limite de bytes para download automático (proteção contra arquivos gigantes).</summary>
    public long MaxDownloadBytes { get; set; } = 52_428_800; // 50 MB

    public OfficialSourceSettings Tuss { get; set; } = new()
    {
        DisplayName = "TUSS (ANS)",
        SourceUrl = "https://www.gov.br/ans/pt-br/assuntos/prestadores/padrao-para-troca-de-informacao-em-saude-suplementar-2013-tiss",
        BundledFolderVersion = "202601",
        AutoImportBundled = true,
    };

    public OfficialSourceSettings Tiss { get; set; } = new()
    {
        DisplayName = "TISS / Layout XML (ANS)",
        SourceUrl = "https://www.gov.br/ans/pt-br/assuntos/prestadores/padrao-para-troca-de-informacao-em-saude-suplementar-2013-tiss",
        ManualDownloadRequired = true,
    };

    public OfficialSourceSettings Sigtap { get; set; } = new()
    {
        DisplayName = "SIGTAP (DATASUS)",
        SourceUrl = "http://sigtap.datasus.gov.br/tabela-unificada/app/download.jsp",
        CheckUrl = "http://sigtap.datasus.gov.br/tabela-unificada/competencias.rss",
        RssFeedUrl = "http://sigtap.datasus.gov.br/tabela-unificada/competencias.rss",
        MinDownloadBytes = 50_000,
        ManualDownloadRequired = false,
        AutoSyncOfficial = true,
    };

    public OfficialSourceSettings Ans { get; set; } = new()
    {
        DisplayName = "ANS — Normativas e Anexos",
        SourceUrl = "https://www.gov.br/ans/pt-br/assuntos/prestadores",
        ManualDownloadRequired = true,
    };

    public OfficialSourceSettings SusTables { get; set; } = new()
    {
        DisplayName = "Tabelas SUS (DATASUS)",
        SourceUrl = "https://datasus.saude.gov.br/transferencia-de-arquivos/",
        ManualDownloadRequired = true,
    };

    public OfficialSourceSettings Anvisa { get; set; } = new()
    {
        DisplayName = "ANVISA — Bulas e Medicamentos",
        SourceUrl = "https://consultas.anvisa.gov.br/",
        ManualDownloadRequired = true,
    };

    public OfficialSourceSettings Brasindice { get; set; } = new()
    {
        DisplayName = "Brasíndice — Preços de Medicamentos",
        SourceUrl = "https://www.brasindice.com.br/",
        ManualDownloadRequired = true,
    };

    public OfficialSourceSettings Simpro { get; set; } = new()
    {
        DisplayName = "SIMPRO — Materiais e OPME",
        SourceUrl = "https://www.simpro.com.br/",
        ManualDownloadRequired = true,
    };
}

public class OfficialSourceSettings
{
    public string DisplayName { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public string? CheckUrl { get; set; }
    public string? ExpectedVersion { get; set; }
    public string? ExpectedFileHash { get; set; }
    public bool ManualDownloadRequired { get; set; }
    public bool AutoImportBundled { get; set; }
    public bool AutoSyncOfficial { get; set; }
    public string? RssFeedUrl { get; set; }
    public long MinDownloadBytes { get; set; } = 50_000;
    public string? BundledFolderVersion { get; set; }
}
