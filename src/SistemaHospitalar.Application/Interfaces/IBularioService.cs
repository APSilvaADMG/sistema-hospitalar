using System.Text.Json;

namespace SistemaHospitalar.Application.Interfaces;

public interface IBularioService
{
    Task<bool> IsAnvisaAvailableAsync(CancellationToken cancellationToken = default);

    Task<JsonDocument?> SearchAsync(string name, int page = 1, CancellationToken cancellationToken = default);

    Task<JsonDocument?> GetMedicationAsync(string processNumber, CancellationToken cancellationToken = default);

    Task<JsonDocument?> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<byte[]?> GetPdfAsync(string bulaId, CancellationToken cancellationToken = default);

    Task<string?> GetPdfUrlAsync(string bulaId, CancellationToken cancellationToken = default);
}
