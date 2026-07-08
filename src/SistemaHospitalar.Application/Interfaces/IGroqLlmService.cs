namespace SistemaHospitalar.Application.Interfaces;

/// <summary>
/// Cliente Groq (OpenAI-compatible) para enriquecimento de insights epidemiológicos.
/// Dados enviados devem ser agregados ou anonimizados — nunca CPF/CNS completos.
/// </summary>
public interface IGroqLlmService
{
    bool IsConfigured { get; }

    Task<string?> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamCompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
