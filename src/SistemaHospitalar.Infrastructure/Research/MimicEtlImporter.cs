using System.Globalization;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using SistemaHospitalar.Application.DTOs.Research;

namespace SistemaHospitalar.Infrastructure.Research;

internal static class MimicVitalItemIds
{
    public static readonly IReadOnlySet<int> ChartEventIds = new HashSet<int>
    {
        220045, // heart rate
        220179, // systolic BP
        220180, // diastolic BP
        220277, // SpO2
        220210, // respiratory rate
        223761  // temperature Celsius
    };
}

public sealed class MimicEtlImporter(
    IOptions<MimicResearchOptions> options,
    IHostEnvironment hostEnvironment,
    ILogger<MimicEtlImporter> logger)
{
    private static readonly string[] ChartEventColumns =
    [
        "SUBJECT_ID", "HADM_ID", "ICUSTAY_ID", "ITEMID", "CHARTTIME", "VALUENUM"
    ];

    public string? ResolveScriptsDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(hostEnvironment.ContentRootPath, "..", "..", "scripts", "mimic"),
            Path.Combine(hostEnvironment.ContentRootPath, "scripts", "mimic"),
            Path.Combine(AppContext.BaseDirectory, "scripts", "mimic")
        };

        foreach (var path in candidates)
        {
            var full = Path.GetFullPath(path);
            if (File.Exists(Path.Combine(full, "001-staging-schema.sql")))
            {
                return full;
            }
        }

        return null;
    }

    public async Task EnsureStagingSchemaAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var scriptsDir = ResolveScriptsDirectory()
            ?? throw new InvalidOperationException("scripts/mimic SQL files not found.");

        var schemaPath = Path.Combine(scriptsDir, "001-staging-schema.sql");
        var sql = await File.ReadAllTextAsync(schemaPath, cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> BeginEtlRunAsync(
        NpgsqlConnection connection,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO mimic_staging.etl_run (status, phase, source_path)
            VALUES ('running', 'chartevents_csv', @source)
            RETURNING id;
            """,
            connection);
        cmd.Parameters.AddWithValue("source", sourcePath);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    public async Task TruncateRawAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand(
            "TRUNCATE mimic_staging.chartevents_vitals_raw;",
            connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<long> ImportChartEventsVitalsAsync(
        NpgsqlConnection connection,
        string chartEventsPath,
        int etlRunId,
        int maxSubjects,
        MimicEtlImportProgress? progress,
        CancellationToken cancellationToken)
    {
        await using var stream = OpenChartEventsStream(chartEventsPath);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var header = await reader.ReadLineAsync(cancellationToken)
            ?? throw new InvalidOperationException("CHARTEVENTS file is empty.");

        var columnIndex = ParseHeader(header);
        var subjectCap = new HashSet<int>();
        long rowsProcessed = 0;
        const int batchSize = 2000;
        var batch = new List<ChartVitalRow>(batchSize);

        progress?.Phase = "streaming_csv";

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (!TryParseRow(line, columnIndex, out var row))
            {
                continue;
            }

            if (!MimicVitalItemIds.ChartEventIds.Contains(row.ItemId))
            {
                continue;
            }

            if (maxSubjects > 0)
            {
                if (!subjectCap.Contains(row.SubjectId))
                {
                    if (subjectCap.Count >= maxSubjects)
                    {
                        continue;
                    }

                    subjectCap.Add(row.SubjectId);
                }
            }

            if (row.ValueNum is null)
            {
                continue;
            }

            batch.Add(row with { EtlRunId = etlRunId });
            rowsProcessed++;

            if (batch.Count >= batchSize)
            {
                await CopyBatchAsync(connection, batch, cancellationToken);
                batch.Clear();
                if (progress is not null)
                {
                    progress.RowsProcessed = rowsProcessed;
                }

                if (rowsProcessed % 50_000 == 0)
                {
                    logger.LogInformation("MIMIC ETL: {Rows} raw vital rows loaded", rowsProcessed);
                }
            }
        }

        if (batch.Count > 0)
        {
            await CopyBatchAsync(connection, batch, cancellationToken);
            if (progress is not null)
            {
                progress.RowsProcessed = rowsProcessed;
            }
        }

        return rowsProcessed;
    }

    public async Task<long> PivotVitalSnapshotsAsync(
        NpgsqlConnection connection,
        int etlRunId,
        CancellationToken cancellationToken)
    {
        var scriptsDir = ResolveScriptsDirectory()
            ?? throw new InvalidOperationException("scripts/mimic SQL files not found.");

        var pivotPath = Path.Combine(scriptsDir, "002-etl-vital-signs.sql");
        var sql = await File.ReadAllTextAsync(pivotPath, cancellationToken);
        sql = sql.Replace(":etl_run_id", etlRunId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

        await using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        await using var countCmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM mimic_staging.vital_sign_snapshot WHERE etl_run_id = @runId;",
            connection);
        countCmd.Parameters.AddWithValue("runId", etlRunId);
        var count = await countCmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(count, CultureInfo.InvariantCulture);
    }

    public async Task CompleteEtlRunAsync(
        NpgsqlConnection connection,
        int etlRunId,
        long rowsProcessed,
        CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand(
            """
            UPDATE mimic_staging.etl_run
            SET status = 'completed',
                completed_at = NOW(),
                rows_processed = @rows,
                phase = 'completed'
            WHERE id = @id;
            """,
            connection);
        cmd.Parameters.AddWithValue("rows", rowsProcessed);
        cmd.Parameters.AddWithValue("id", etlRunId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task FailEtlRunAsync(
        NpgsqlConnection connection,
        int etlRunId,
        string error,
        CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand(
            """
            UPDATE mimic_staging.etl_run
            SET status = 'failed',
                completed_at = NOW(),
                error_message = @error
            WHERE id = @id;
            """,
            connection);
        cmd.Parameters.AddWithValue("error", error);
        cmd.Parameters.AddWithValue("id", etlRunId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public string? ResolveChartEventsPath()
    {
        var csvRoot = options.Value.CsvPath;
        if (string.IsNullOrWhiteSpace(csvRoot))
        {
            return null;
        }

        var gz = Path.Combine(csvRoot, "CHARTEVENTS.csv.gz");
        if (File.Exists(gz))
        {
            return gz;
        }

        var csv = Path.Combine(csvRoot, "CHARTEVENTS.csv");
        return File.Exists(csv) ? csv : null;
    }

    private static Stream OpenChartEventsStream(string path)
    {
        var fileStream = File.OpenRead(path);
        if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            return new GZipStream(fileStream, CompressionMode.Decompress);
        }

        return fileStream;
    }

    private static Dictionary<string, int> ParseHeader(string header)
    {
        var parts = header.Split(',');
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < parts.Length; i++)
        {
            map[parts[i].Trim()] = i;
        }

        foreach (var required in ChartEventColumns)
        {
            if (!map.ContainsKey(required))
            {
                throw new InvalidOperationException($"CHARTEVENTS missing column {required}.");
            }
        }

        return map;
    }

    private static bool TryParseRow(string line, Dictionary<string, int> columnIndex, out ChartVitalRow row)
    {
        row = default;
        var parts = line.Split(',');
        var maxIndex = columnIndex.Values.Max();
        if (parts.Length <= maxIndex)
        {
            return false;
        }

        if (!int.TryParse(parts[columnIndex["SUBJECT_ID"]], out var subjectId))
        {
            return false;
        }

        if (!int.TryParse(parts[columnIndex["ITEMID"]], out var itemId))
        {
            return false;
        }

        int? hadmId = int.TryParse(parts[columnIndex["HADM_ID"]], out var hadm) ? hadm : null;
        int? icuStayId = int.TryParse(parts[columnIndex["ICUSTAY_ID"]], out var icu) ? icu : null;

        if (!DateTime.TryParse(parts[columnIndex["CHARTTIME"]], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var chartTime))
        {
            return false;
        }

        decimal? valueNum = null;
        var valueText = parts[columnIndex["VALUENUM"]];
        if (!string.IsNullOrWhiteSpace(valueText) &&
            decimal.TryParse(valueText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            valueNum = parsed;
        }

        row = new ChartVitalRow(subjectId, hadmId, icuStayId, itemId, chartTime, valueNum, 0);
        return true;
    }

    private static async Task CopyBatchAsync(
        NpgsqlConnection connection,
        IReadOnlyList<ChartVitalRow> batch,
        CancellationToken cancellationToken)
    {
        const string copySql =
            """
            COPY mimic_staging.chartevents_vitals_raw
            (subject_id, hadm_id, icustay_id, itemid, charttime, valuenum, etl_run_id)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var writer = await connection.BeginBinaryImportAsync(copySql, cancellationToken);
        foreach (var row in batch)
        {
            await writer.StartRowAsync(cancellationToken);
            await writer.WriteAsync(row.SubjectId, NpgsqlDbType.Integer, cancellationToken);
            if (row.HadmId is int hadm)
            {
                await writer.WriteAsync(hadm, NpgsqlDbType.Integer, cancellationToken);
            }
            else
            {
                await writer.WriteNullAsync(cancellationToken);
            }

            if (row.IcuStayId is int icu)
            {
                await writer.WriteAsync(icu, NpgsqlDbType.Integer, cancellationToken);
            }
            else
            {
                await writer.WriteNullAsync(cancellationToken);
            }

            await writer.WriteAsync(row.ItemId, NpgsqlDbType.Integer, cancellationToken);
            await writer.WriteAsync(row.ChartTime, NpgsqlDbType.TimestampTz, cancellationToken);
            if (row.ValueNum is decimal value)
            {
                await writer.WriteAsync(value, NpgsqlDbType.Numeric, cancellationToken);
            }
            else
            {
                await writer.WriteNullAsync(cancellationToken);
            }

            await writer.WriteAsync(row.EtlRunId, NpgsqlDbType.Integer, cancellationToken);
        }

        await writer.CompleteAsync(cancellationToken);
    }

    private readonly record struct ChartVitalRow(
        int SubjectId,
        int? HadmId,
        int? IcuStayId,
        int ItemId,
        DateTime ChartTime,
        decimal? ValueNum,
        int EtlRunId);
}
