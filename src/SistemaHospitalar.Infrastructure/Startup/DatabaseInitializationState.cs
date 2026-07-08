namespace SistemaHospitalar.Infrastructure.Startup;

/// <summary>
/// Indica se a inicialização do banco (migrations + seeds) terminou com sucesso.
/// </summary>
public sealed class DatabaseInitializationState
{
    public bool IsComplete { get; private set; }
    public string? LastError { get; private set; }

    public void MarkComplete() => IsComplete = true;

    public void MarkFailed(Exception ex)
    {
        LastError = ex.Message;
        IsComplete = false;
    }
}
