using SistemaHospitalar.Application.DTOs.Research;

namespace SistemaHospitalar.Infrastructure.Research;

/// <summary>Tracks in-process ETL state for dev import triggers.</summary>
public sealed class MimicEtlStateHolder
{
    private readonly object _gate = new();
    private MimicEtlImportProgress? _progress;

    public bool TryBeginImport(out MimicEtlImportProgress progress)
    {
        lock (_gate)
        {
            if (_progress is { IsRunning: true })
            {
                progress = _progress;
                return false;
            }

            progress = new MimicEtlImportProgress();
            _progress = progress;
            return true;
        }
    }

    public MimicEtlImportProgress? GetProgress()
    {
        lock (_gate)
        {
            return _progress;
        }
    }

    public void Complete(MimicEtlImportProgress progress)
    {
        lock (_gate)
        {
            if (ReferenceEquals(_progress, progress))
            {
                progress.MarkCompleted();
            }
        }
    }

    public void Fail(MimicEtlImportProgress progress, string error)
    {
        lock (_gate)
        {
            if (ReferenceEquals(_progress, progress))
            {
                progress.MarkFailed(error);
            }
        }
    }
}

public sealed class MimicEtlImportProgress
{
    public int? RunId { get; set; }
    public string Phase { get; set; } = "starting";
    public long RowsProcessed { get; set; }
    public bool IsRunning { get; private set; } = true;
    public string? Error { get; private set; }

    public void MarkCompleted() => IsRunning = false;

    public void MarkFailed(string error)
    {
        Error = error;
        IsRunning = false;
    }
}
