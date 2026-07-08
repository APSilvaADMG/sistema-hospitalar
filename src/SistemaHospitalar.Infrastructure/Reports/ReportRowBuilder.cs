namespace SistemaHospitalar.Infrastructure.Reports;

internal static class ReportRowBuilder
{
    public static Dictionary<string, object?> Row(params (string Key, object? Value)[] columns) =>
        columns.ToDictionary(c => c.Key, c => c.Value);

    public static List<Dictionary<string, object?>> From<T>(
        IEnumerable<T> items,
        Func<T, Dictionary<string, object?>> projector) =>
        items.Select(projector).ToList();
}
