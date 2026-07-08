using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SistemaHospitalar.Infrastructure.Connect;

public interface IConnectAttachmentStorage
{
    Task<string> SaveAsync(Guid messageId, Guid attachmentId, string fileName, byte[] content, CancellationToken cancellationToken = default);
    Task<(byte[] Content, string MimeType)?> TryReadAsync(string storagePath, CancellationToken cancellationToken = default);
    void DeleteIfExists(string? storagePath);
}

public class ConnectAttachmentStorage(
    IHostEnvironment environment,
    IOptions<ConnectSettings> settings) : IConnectAttachmentStorage
{
    private string UploadRoot =>
        Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "connect");

    public async Task<string> SaveAsync(
        Guid messageId,
        Guid attachmentId,
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        if (content.Length > settings.Value.MaxAttachmentBytes)
        {
            throw new InvalidOperationException(
                $"Anexo excede o limite de {settings.Value.MaxAttachmentBytes / (1024 * 1024)} MB.");
        }

        var safeName = Path.GetFileName(fileName);
        var dir = Path.Combine(UploadRoot, messageId.ToString("N"));
        Directory.CreateDirectory(dir);

        var diskName = $"{attachmentId:N}_{safeName}";
        var fullPath = Path.Combine(dir, diskName);
        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);

        return Path.Combine("uploads", "connect", messageId.ToString("N"), diskName)
            .Replace('\\', '/');
    }

    public async Task<(byte[] Content, string MimeType)?> TryReadAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath)) return null;

        var normalized = storagePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(environment.ContentRootPath, "wwwroot", normalized);
        if (!File.Exists(fullPath)) return null;

        var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
        var mime = GuessMimeType(Path.GetFileName(fullPath));
        return (bytes, mime);
    }

    public void DeleteIfExists(string? storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath)) return;

        var normalized = storagePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(environment.ContentRootPath, "wwwroot", normalized);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private static string GuessMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream",
        };
    }
}
