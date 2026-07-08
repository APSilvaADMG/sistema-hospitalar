namespace SistemaHospitalar.Infrastructure.Tiss;

public static class TissBundledCatalogLocator
{
    public static string? ResolveFolder202601()
    {
        foreach (var root in GetSearchRoots())
        {
            var candidate = Path.Combine(root, "Diversos", "TISS", "202601");
            if (Directory.Exists(candidate))
                return candidate;
        }

        return null;
    }

    public static IReadOnlyList<string> FindTussXlsxFiles(string folder202601)
    {
        return Directory
            .EnumerateFiles(folder202601, "*.xlsx", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).StartsWith("~$", StringComparison.Ordinal))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> GetSearchRoots()
    {
        yield return Directory.GetCurrentDirectory();

        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            yield return dir;
            var parent = Path.GetDirectoryName(dir);
            if (string.IsNullOrEmpty(parent) || parent == dir)
                break;
            dir = parent;
        }
    }
}
