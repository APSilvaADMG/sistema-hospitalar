using System.Text.Json;
using System.Net.Http.Json;
using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Tiss;

public class MockOperatorTissClient : IOperatorTissClient
{
    public Task<OperatorEligibilityResponse> CheckEligibilityAsync(
        HealthInsurance insurer,
        EligibilityCheckRequest request,
        PatientInsurance? patientInsurance,
        CancellationToken cancellationToken = default)
    {
        var profile = OperatorIntegrationProfiles.FindByOperatorCode(insurer.OperatorCode)
            ?? OperatorIntegrationProfiles.FindByName(insurer.Name);
        var code = insurer.OperatorCode ?? profile?.OperatorCode ?? "OPERADORA";
        var eligible = patientInsurance is not null;

        var message = eligible
            ? profile?.OperatorCode switch
            {
                "BRADESCO" => $"Bradesco Saúde: beneficiário ativo. Plano {patientInsurance?.PlanName ?? "—"}.",
                "AMIL" => $"Amil: elegível. Acomodação {patientInsurance?.AccommodationType ?? "enfermaria"}.",
                "SULAMERICA" => $"SulAmérica: carteirinha válida. Cobertura nacional.",
                "HAPVIDA" => $"Hapvida: beneficiário ativo no sistema.",
                "UNIMED" => $"Unimed: cooperado elegível — rede credenciada.",
                "PORTO" => $"Porto Seguro Saúde: plano ativo.",
                _ => $"[{code}] Beneficiário elegível — plano ativo.",
            }
            : $"[{code}] Carteirinha não localizada. Verifique número e operadora.";

        var response = new OperatorEligibilityResponse(
            eligible,
            message,
            patientInsurance?.PlanName ?? "Plano Mock",
            eligible ? (profile?.BusinessRules ?? "Cobertura ambulatorial e hospitalar conforme contrato.") : null,
            patientInsurance?.ValidUntil?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            JsonSerializer.Serialize(new { mock = true, operadora = insurer.Name, codigo = code, elegivel = eligible }));

        return Task.FromResult(response);
    }

    public Task<OperatorAuthorizationResponse> RequestAuthorizationAsync(
        HealthInsurance insurer,
        CreateAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = OperatorIntegrationProfiles.FindByOperatorCode(insurer.OperatorCode)
            ?? OperatorIntegrationProfiles.FindByName(insurer.Name);
        var code = insurer.OperatorCode ?? profile?.OperatorCode ?? "OP";
        var prefix = code switch
        {
            "BRADESCO" => "BRA",
            "AMIL" => "AML",
            "SULAMERICA" => "SUL",
            "HAPVIDA" => "HAP",
            "UNIMED" => "UNI",
            "PORTO" => "POR",
            "GNDI" => "GND",
            "GOLDENCROSS" => "GCX",
            _ => code.Length >= 3 ? code[..3] : "AUT",
        };

        var number = string.IsNullOrWhiteSpace(request.AuthorizationNumber)
            ? $"{prefix}{DateTime.UtcNow:yyMMdd}{Random.Shared.Next(10000, 99999)}"
            : request.AuthorizationNumber.Trim();

        var typeLabel = request.AuthorizationType.ToString();
        var message = profile?.OperatorCode switch
        {
            "BRADESCO" => $"Bradesco: autorização {typeLabel} aprovada. Senha {number}.",
            "AMIL" => $"Amil: guia autorizada. Validade conforme contrato.",
            "SULAMERICA" => $"SulAmérica: procedimento autorizado.",
            "HAPVIDA" => $"Hapvida: senha liberada para atendimento.",
            "UNIMED" => $"Unimed: autorização registrada na singular.",
            _ => $"[{code}] Autorização {typeLabel} aprovada online.",
        };

        return Task.FromResult(new OperatorAuthorizationResponse(
            true,
            number,
            message,
            JsonSerializer.Serialize(new { mock = true, operadora = insurer.Name, senha = number, tipo = typeLabel })));
    }

    public Task<OperatorBatchSendResponse> SendBatchAsync(
        HealthInsurance insurer,
        TissBatch batch,
        IReadOnlyList<TissGuide> guides,
        CancellationToken cancellationToken = default)
    {
        var protocol = $"PROT-{insurer.OperatorCode}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        return Task.FromResult(new OperatorBatchSendResponse(
            true,
            protocol,
            $"[MOCK {insurer.OperatorCode}] Lote {batch.BatchNumber} recebido com {guides.Count} guia(s).",
            JsonSerializer.Serialize(new { mock = true, protocol, lote = batch.BatchNumber })));
    }

    public Task<OperatorDemonstrativoResponse> FetchDemonstrativoAsync(
        HealthInsurance insurer,
        TissBatch? batch,
        string competence,
        CancellationToken cancellationToken = default)
    {
        var guides = batch?.Guides?.Where(g => g.IsActive).ToList() ?? [];
        var lines = guides.Select(g => new OperatorDemonstrativoLine(
            g.GuideNumber,
            g.Items.FirstOrDefault(i => i.IsActive)?.TussCode,
            g.TotalAmount,
            Math.Round(g.TotalAmount * 0.92m, 2),
            Math.Round(g.TotalAmount * 0.08m, 2),
            g.TotalAmount > 0 ? "1301 - Procedimento não autorizado" : null,
            g.TotalAmount > 0 ? "1301" : null)).ToList();

        var demoNumber = $"DP-{insurer.OperatorCode}-{competence.Replace("-", "")}-{Random.Shared.Next(1000, 9999)}";
        return Task.FromResult(new OperatorDemonstrativoResponse(
            true,
            demoNumber,
            lines,
            JsonSerializer.Serialize(new { mock = true, demonstrativo = demoNumber, linhas = lines.Count })));
    }
}

public class HttpOperatorTissClient(HttpClient httpClient) : IOperatorTissClient
{
    public async Task<OperatorEligibilityResponse> CheckEligibilityAsync(
        HealthInsurance insurer,
        EligibilityCheckRequest request,
        PatientInsurance? patientInsurance,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(insurer.WebServiceUrl))
            throw new InvalidOperationException("URL do webservice não configurada para a operadora.");

        var response = await PostAsync(insurer, "eligibility", request, cancellationToken);
        return new OperatorEligibilityResponse(true, response, null, null, null, response);
    }

    public async Task<OperatorAuthorizationResponse> RequestAuthorizationAsync(
        HealthInsurance insurer,
        CreateAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(insurer.WebServiceUrl))
            throw new InvalidOperationException("URL do webservice não configurada para a operadora.");

        var response = await PostAsync(insurer, "authorization", request, cancellationToken);
        return new OperatorAuthorizationResponse(true, request.AuthorizationNumber, response, response);
    }

    public async Task<OperatorBatchSendResponse> SendBatchAsync(
        HealthInsurance insurer,
        TissBatch batch,
        IReadOnlyList<TissGuide> guides,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(insurer.WebServiceUrl))
            throw new InvalidOperationException("URL do webservice não configurada para a operadora.");

        var payload = new { batchId = batch.Id, batch.BatchNumber, guideCount = guides.Count };
        var response = await PostAsync(insurer, "batch/send", payload, cancellationToken);
        return new OperatorBatchSendResponse(true, batch.ProtocolNumber ?? batch.BatchNumber, response, response);
    }

    public async Task<OperatorDemonstrativoResponse> FetchDemonstrativoAsync(
        HealthInsurance insurer,
        TissBatch? batch,
        string competence,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(insurer.WebServiceUrl))
            throw new InvalidOperationException("URL do webservice não configurada para a operadora.");

        var response = await PostAsync(insurer, "demonstrativo/fetch", new { competence, batchId = batch?.Id }, cancellationToken);
        return new OperatorDemonstrativoResponse(true, $"DP-HTTP-{competence}", [], response);
    }

    private async Task<string> PostAsync(
        HealthInsurance insurer,
        string path,
        object payload,
        CancellationToken cancellationToken)
    {
        var url = $"{insurer.WebServiceUrl!.TrimEnd('/')}/{path}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = JsonContent.Create(payload);
        if (!string.IsNullOrWhiteSpace(insurer.IntegrationUser))
            request.Headers.TryAddWithoutValidation("X-Integration-User", insurer.IntegrationUser);
        if (!string.IsNullOrWhiteSpace(insurer.IntegrationSecret))
            request.Headers.TryAddWithoutValidation("X-Integration-Secret", insurer.IntegrationSecret);

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}

public static class OperatorTissClientFactory
{
    public static IOperatorTissClient Resolve(
        HealthInsurance insurer,
        MockOperatorTissClient mock,
        HttpOperatorTissClient http)
        => insurer.UseMockIntegration || string.IsNullOrWhiteSpace(insurer.WebServiceUrl) ? mock : http;
}

public static class OperatorTransactionLogger
{
    public static async Task LogAsync(
        AppDbContext db,
        HealthInsurance insurer,
        OperatorTransactionType type,
        OperatorTransactionStatus status,
        string? referenceId,
        string? request,
        string? response,
        string? error,
        int durationMs,
        CancellationToken cancellationToken)
    {
        db.OperatorTransactionLogs.Add(new OperatorTransactionLog
        {
            HealthInsuranceId = insurer.Id,
            TransactionType = type,
            Status = status,
            ReferenceId = referenceId,
            RequestPayload = request,
            ResponsePayload = response,
            ErrorMessage = error,
            DurationMs = durationMs,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
