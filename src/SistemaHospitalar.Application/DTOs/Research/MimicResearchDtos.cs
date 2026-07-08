namespace SistemaHospitalar.Application.DTOs.Research;

public record MimicResearchStatusDto(
    bool Enabled,
    bool DatabaseConfigured,
    bool DatabaseReachable,
    string DisplayLabel,
    string Warning,
    IReadOnlyList<MimicSampleQueryDto> SampleQueries,
    MimicEtlStatusDto? Etl);

public record MimicSampleQueryDto(
    string Id,
    string Title,
    string Description,
    string Sql);

public record MimicEtlStatusDto(
    bool StagingSchemaReady,
    long RawVitalRows,
    long SnapshotRows,
    int? LastRunId,
    string? LastRunStatus,
    DateTime? LastRunStartedAt,
    DateTime? LastRunCompletedAt,
    long? LastRunRowsProcessed,
    string? LastRunError,
    bool ImportInProgress,
    string? CurrentPhase,
    long? CurrentRowsProcessed);

public record MimicVitalSignDto(
    long Id,
    int SubjectId,
    int? HadmId,
    int? IcuStayId,
    DateTime RecordedAt,
    int? HeartRate,
    int? SystolicBp,
    int? DiastolicBp,
    int? SpO2,
    int? RespiratoryRate,
    decimal? TemperatureC,
    string Source);

public record MimicEtlTriggerResultDto(
    bool Accepted,
    string Message,
    int? RunId);

public record MimicVitalsQueryResultDto(
    int SubjectId,
    int Count,
    string DisplayLabel,
    string Warning,
    IReadOnlyList<MimicVitalSignDto> Records);
