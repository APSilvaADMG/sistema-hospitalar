using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SistemaHospitalar.Application.DTOs.Research;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Infrastructure.Research;

public class MimicResearchService(
    IOptions<MimicResearchOptions> options,
    MimicEtlImporter etlImporter,
    MimicEtlStateHolder etlState,
    IHostEnvironment hostEnvironment,
    ILogger<MimicResearchService> logger) : IMimicResearchService
{
    private const string LegalWarning =
        "MIMIC-III: uso apenas em ambiente de pesquisa isolado. Proibido misturar com PHI real ou reidentificar pacientes (DUA PhysioNet).";

    private static readonly IReadOnlyList<MimicSampleQueryDto> SampleQueries =
    [
        new(
            "icu_census",
            "Censo UTI por unidade",
            "Contagem de estadias ICU ativas por FIRST_CAREUNIT (schema nativo MIMIC).",
            """
            SELECT first_careunit, COUNT(*) AS stays
            FROM icustays
            WHERE outtime IS NULL
            GROUP BY first_careunit
            ORDER BY stays DESC
            LIMIT 20;
            """),
        new(
            "avg_los_icu",
            "Tempo médio de permanência na UTI (horas)",
            "Média de duração das estadias ICU concluídas.",
            """
            SELECT ROUND(AVG(EXTRACT(EPOCH FROM (outtime - intime)) / 3600.0)::numeric, 1) AS avg_hours
            FROM icustays
            WHERE outtime IS NOT NULL;
            """),
        new(
            "lab_creatinine",
            "Creatinina — últimos resultados por admissão",
            "Exemplo de join LABEVENTS + D_LABITEMS (ajuste ITEMID conforme dicionário).",
            """
            SELECT l.hadm_id, l.charttime, l.valuenum, l.valueuom
            FROM labevents l
            JOIN d_labitems d ON l.itemid = d.itemid
            WHERE LOWER(d.label) LIKE '%creatinine%'
            ORDER BY l.charttime DESC
            LIMIT 50;
            """),
        new(
            "staging_vitals",
            "Sinais vitais (staging ETL)",
            "Snapshots wide-format em mimic_staging.vital_sign_snapshot.",
            """
            SELECT subject_id, icustay_id, recorded_at,
                   heart_rate, systolic_bp, diastolic_bp, spo2, respiratory_rate, temperature_c
            FROM mimic_staging.vital_sign_snapshot
            WHERE subject_id = :subject_id
            ORDER BY recorded_at DESC
            LIMIT 50;
            """)
    ];

    public async Task<MimicResearchStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var cfg = options.Value;
        var configured = !string.IsNullOrWhiteSpace(cfg.ConnectionString);
        var reachable = false;
        MimicEtlStatusDto? etl = null;

        if (cfg.Enabled && configured)
        {
            if (IsProductionDatabase(cfg.ConnectionString!))
            {
                return new MimicResearchStatusDto(
                    Enabled: true,
                    DatabaseConfigured: true,
                    DatabaseReachable: false,
                    DisplayLabel: cfg.DisplayLabel,
                    Warning: "ConnectionString aponta para sistema_hospitalar — use banco mimic_iii isolado.",
                    SampleQueries: SampleQueries,
                    Etl: null);
            }

            try
            {
                await using var conn = await OpenConnectionAsync(cancellationToken);
                reachable = true;
                etl = await ReadEtlStatusAsync(conn, cancellationToken);
            }
            catch
            {
                reachable = false;
            }
        }

        return new MimicResearchStatusDto(
            Enabled: cfg.Enabled,
            DatabaseConfigured: configured,
            DatabaseReachable: reachable,
            DisplayLabel: cfg.DisplayLabel,
            Warning: LegalWarning,
            SampleQueries: SampleQueries,
            Etl: etl);
    }

    public async Task<MimicEtlStatusDto> GetEtlStatusAsync(CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        await using var conn = await OpenConnectionAsync(cancellationToken);
        return await ReadEtlStatusAsync(conn, cancellationToken);
    }

    public async Task<MimicVitalsQueryResultDto> GetVitalsAsync(
        int subjectId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        limit = Math.Clamp(limit, 1, 500);

        await using var conn = await OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(
            """
            SELECT id, subject_id, hadm_id, icustay_id, recorded_at,
                   heart_rate, systolic_bp, diastolic_bp, spo2, respiratory_rate, temperature_c, source
            FROM mimic_staging.vital_sign_snapshot
            WHERE subject_id = @subjectId
            ORDER BY recorded_at DESC
            LIMIT @limit;
            """,
            conn);
        cmd.Parameters.AddWithValue("subjectId", subjectId);
        cmd.Parameters.AddWithValue("limit", limit);

        var records = new List<MimicVitalSignDto>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new MimicVitalSignDto(
                Id: reader.GetInt64(0),
                SubjectId: reader.GetInt32(1),
                HadmId: reader.IsDBNull(2) ? null : reader.GetInt32(2),
                IcuStayId: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                RecordedAt: reader.GetDateTime(4),
                HeartRate: reader.IsDBNull(5) ? null : reader.GetInt16(5),
                SystolicBp: reader.IsDBNull(6) ? null : reader.GetInt16(6),
                DiastolicBp: reader.IsDBNull(7) ? null : reader.GetInt16(7),
                SpO2: reader.IsDBNull(8) ? null : reader.GetInt16(8),
                RespiratoryRate: reader.IsDBNull(9) ? null : reader.GetInt16(9),
                TemperatureC: reader.IsDBNull(10) ? null : reader.GetDecimal(10),
                Source: reader.GetString(11)));
        }

        return new MimicVitalsQueryResultDto(
            SubjectId: subjectId,
            Count: records.Count,
            DisplayLabel: options.Value.DisplayLabel,
            Warning: LegalWarning,
            Records: records);
    }

    public Task<MimicEtlTriggerResultDto> TriggerSubsetImportAsync(
        int? maxSubjects = null,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();

        if (!hostEnvironment.IsDevelopment())
        {
            return Task.FromResult(new MimicEtlTriggerResultDto(
                Accepted: false,
                Message: "Importação via API permitida apenas em Development.",
                RunId: null));
        }

        var cfg = options.Value;
        if (!cfg.AllowDevImportTrigger)
        {
            return Task.FromResult(new MimicEtlTriggerResultDto(
                Accepted: false,
                Message: "MimicResearch:AllowDevImportTrigger está desabilitado.",
                RunId: null));
        }

        var chartPath = etlImporter.ResolveChartEventsPath();
        if (chartPath is null)
        {
            return Task.FromResult(new MimicEtlTriggerResultDto(
                Accepted: false,
                Message: "Configure MimicResearch:CsvPath com CHARTEVENTS.csv(.gz) do download credenciado.",
                RunId: null));
        }

        if (!etlState.TryBeginImport(out var progress))
        {
            return Task.FromResult(new MimicEtlTriggerResultDto(
                Accepted: false,
                Message: "Importação já em andamento.",
                RunId: progress.RunId));
        }

        var cap = maxSubjects ?? cfg.MaxSubjects;
        _ = RunImportInBackgroundAsync(chartPath, cap, progress, cancellationToken);

        return Task.FromResult(new MimicEtlTriggerResultDto(
            Accepted: true,
            Message: "Importação subset iniciada em background. Consulte GET /api/research/mimic/etl/status.",
            RunId: null));
    }

    private async Task RunImportInBackgroundAsync(
        string chartPath,
        int maxSubjects,
        MimicEtlImportProgress progress,
        CancellationToken cancellationToken)
    {
        int? runId = null;
        try
        {
            await using var conn = await OpenConnectionAsync(cancellationToken);
            progress.Phase = "schema";
            await etlImporter.EnsureStagingSchemaAsync(conn, cancellationToken);

            progress.Phase = "etl_run";
            runId = await etlImporter.BeginEtlRunAsync(conn, chartPath, cancellationToken);
            progress.RunId = runId;

            await etlImporter.TruncateRawAsync(conn, cancellationToken);

            progress.Phase = "streaming_csv";
            var rawRows = await etlImporter.ImportChartEventsVitalsAsync(
                conn, chartPath, runId.Value, maxSubjects, progress, cancellationToken);

            progress.Phase = "pivot";
            var snapshots = await etlImporter.PivotVitalSnapshotsAsync(conn, runId.Value, cancellationToken);
            await etlImporter.CompleteEtlRunAsync(conn, runId.Value, rawRows, cancellationToken);

            progress.RowsProcessed = rawRows;
            etlState.Complete(progress);
            logger.LogInformation(
                "MIMIC ETL completed run {RunId}: {RawRows} raw rows, {Snapshots} snapshots",
                runId, rawRows, snapshots);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MIMIC ETL import failed");
            if (runId is int id)
            {
                try
                {
                    await using var conn = await OpenConnectionAsync(cancellationToken);
                    await etlImporter.FailEtlRunAsync(conn, id, ex.Message, cancellationToken);
                }
                catch (Exception inner)
                {
                    logger.LogWarning(inner, "Failed to mark ETL run {RunId} as failed", runId);
                }
            }

            etlState.Fail(progress, ex.Message);
        }
    }

    private void EnsureEnabled()
    {
        var cfg = options.Value;
        if (!cfg.Enabled)
        {
            throw new InvalidOperationException("MimicResearch está desabilitado.");
        }

        if (string.IsNullOrWhiteSpace(cfg.ConnectionString))
        {
            throw new InvalidOperationException("MimicResearch:ConnectionString não configurada.");
        }

        if (IsProductionDatabase(cfg.ConnectionString))
        {
            throw new InvalidOperationException("ConnectionString não pode apontar para sistema_hospitalar.");
        }
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var conn = new NpgsqlConnection(options.Value.ConnectionString);
        await conn.OpenAsync(cancellationToken);
        return conn;
    }

    private static bool IsProductionDatabase(string connectionString) =>
        connectionString.Contains("sistema_hospitalar", StringComparison.OrdinalIgnoreCase);

    private async Task<MimicEtlStatusDto> ReadEtlStatusAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        var stagingReady = await TableExistsAsync(connection, "mimic_staging", "etl_run", cancellationToken);
        if (!stagingReady)
        {
            return new MimicEtlStatusDto(
                StagingSchemaReady: false,
                RawVitalRows: 0,
                SnapshotRows: 0,
                LastRunId: null,
                LastRunStatus: null,
                LastRunStartedAt: null,
                LastRunCompletedAt: null,
                LastRunRowsProcessed: null,
                LastRunError: null,
                ImportInProgress: etlState.GetProgress()?.IsRunning ?? false,
                CurrentPhase: etlState.GetProgress()?.Phase,
                CurrentRowsProcessed: etlState.GetProgress()?.RowsProcessed);
        }

        long rawRows = 0;
        long snapshotRows = 0;
        if (await TableExistsAsync(connection, "mimic_staging", "chartevents_vitals_raw", cancellationToken))
        {
            rawRows = await ScalarLongAsync(connection, "SELECT COUNT(*) FROM mimic_staging.chartevents_vitals_raw;", cancellationToken);
        }

        if (await TableExistsAsync(connection, "mimic_staging", "vital_sign_snapshot", cancellationToken))
        {
            snapshotRows = await ScalarLongAsync(connection, "SELECT COUNT(*) FROM mimic_staging.vital_sign_snapshot;", cancellationToken);
        }

        int? lastRunId = null;
        string? lastStatus = null;
        DateTime? startedAt = null;
        DateTime? completedAt = null;
        long? rowsProcessed = null;
        string? error = null;

        await using (var cmd = new NpgsqlCommand(
            """
            SELECT id, status, started_at, completed_at, rows_processed, error_message
            FROM mimic_staging.etl_run
            ORDER BY id DESC
            LIMIT 1;
            """,
            connection))
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                lastRunId = reader.GetInt32(0);
                lastStatus = reader.GetString(1);
                startedAt = reader.GetDateTime(2);
                completedAt = reader.IsDBNull(3) ? null : reader.GetDateTime(3);
                rowsProcessed = reader.IsDBNull(4) ? null : reader.GetInt64(4);
                error = reader.IsDBNull(5) ? null : reader.GetString(5);
            }
        }

        var progress = etlState.GetProgress();
        return new MimicEtlStatusDto(
            StagingSchemaReady: true,
            RawVitalRows: rawRows,
            SnapshotRows: snapshotRows,
            LastRunId: lastRunId,
            LastRunStatus: lastStatus,
            LastRunStartedAt: startedAt,
            LastRunCompletedAt: completedAt,
            LastRunRowsProcessed: rowsProcessed,
            LastRunError: error,
            ImportInProgress: progress?.IsRunning ?? false,
            CurrentPhase: progress?.Phase,
            CurrentRowsProcessed: progress?.RowsProcessed);
    }

    private static async Task<bool> TableExistsAsync(
        NpgsqlConnection connection,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT EXISTS (
              SELECT 1 FROM information_schema.tables
              WHERE table_schema = @schema AND table_name = @table
            );
            """,
            connection);
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }

    private static async Task<long> ScalarLongAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand(sql, connection);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? 0 : Convert.ToInt64(result);
    }
}
