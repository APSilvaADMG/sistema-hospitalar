using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

/// <summary>Versão instalada e disponível por fonte oficial (ANS, DATASUS, etc.).</summary>
public class OfficialVersion : BaseEntity
{
    public OfficialSourceType SourceType { get; set; }
    public string VersionLabel { get; set; } = string.Empty;
    public string? RemoteVersionLabel { get; set; }
    public string? InstalledFileHash { get; set; }
    public string? RemoteFileHash { get; set; }
    public string? SourceUrl { get; set; }
    public OfficialVersionStatus Status { get; set; } = OfficialVersionStatus.NeverChecked;
    public DateTime? LastCheckedAt { get; set; }
    public DateTime? LastImportedAt { get; set; }
    public int? InstalledRecordCount { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Auditoria de verificações, downloads e importações governamentais.</summary>
public class IntegrationLog : BaseEntity
{
    public OfficialSourceType SourceType { get; set; }
    public string Action { get; set; } = string.Empty;
    public IntegrationLogStatus Status { get; set; } = IntegrationLogStatus.Info;
    public string Message { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
    public long? DurationMs { get; set; }
    public string? TriggeredBy { get; set; }
}
