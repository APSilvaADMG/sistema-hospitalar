namespace SistemaHospitalar.Infrastructure.Research;

public class MimicResearchOptions
{
    public const string SectionName = "MimicResearch";

    /// <summary>Must remain false in production unless a dedicated sandbox DB is used.</summary>
    public bool Enabled { get; set; }

    /// <summary>PostgreSQL connection to isolated mimic_iii database — never DefaultConnection.</summary>
    public string? ConnectionString { get; set; }

    public bool RequireSeparateDatabase { get; set; } = true;

    public string DisplayLabel { get; set; } =
        "Dados de demonstração — MIMIC-III (não são pacientes deste hospital)";

    /// <summary>Path to credentialed MIMIC-III CSV folder (user-provided; not downloaded by the app).</summary>
    public string? CsvPath { get; set; }

    /// <summary>Cap distinct SUBJECT_ID values when streaming CHARTEVENTS (0 = no cap).</summary>
    public int MaxSubjects { get; set; } = 50;

    /// <summary>Allow POST /api/research/mimic/etl/import in Development only.</summary>
    public bool AllowDevImportTrigger { get; set; } = true;
}
