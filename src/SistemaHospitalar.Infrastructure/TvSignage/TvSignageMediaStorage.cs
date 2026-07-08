using Microsoft.Extensions.Hosting;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Infrastructure.TvSignage;

public class TvSignageMediaStorage(IHostEnvironment environment) : ITvSignageMediaStorage
{
    private string UploadRoot =>
        Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "tv");

    public async Task<string> SaveAsync(Guid mediaId, string fileName, byte[] content, CancellationToken cancellationToken = default)
    {
        var safeName = Path.GetFileName(fileName);
        var dir = Path.Combine(UploadRoot, mediaId.ToString("N"));
        Directory.CreateDirectory(dir);
        var diskName = safeName;
        var fullPath = Path.Combine(dir, diskName);
        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);
        return Path.Combine("uploads", "tv", mediaId.ToString("N"), diskName).Replace('\\', '/');
    }

    public void DeleteIfExists(string? storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath)) return;
        var normalized = storagePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(environment.ContentRootPath, "wwwroot", normalized);
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }
}
