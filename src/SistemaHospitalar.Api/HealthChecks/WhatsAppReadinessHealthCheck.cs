using Microsoft.Extensions.Diagnostics.HealthChecks;
using SistemaHospitalar.Infrastructure.Connect;

namespace SistemaHospitalar.Api.HealthChecks;

public sealed class WhatsAppReadinessHealthCheck(WhatsAppHealthService healthService) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var report = healthService.GetReport();

        if (!report.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "WhatsApp Connect desabilitado.",
                data: BuildData(report)));
        }

        if (report.Ready)
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                report.UseMockProvider ? "WhatsApp em modo mock (desenvolvimento)." : "WhatsApp Meta configurado.",
                data: BuildData(report)));
        }

        return Task.FromResult(HealthCheckResult.Degraded(
            string.Join(" ", report.Issues),
            data: BuildData(report)));
    }

    private static IReadOnlyDictionary<string, object> BuildData(WhatsAppHealthReport report)
        => new Dictionary<string, object>
        {
            ["provider"] = report.ProviderName,
            ["mock"] = report.UseMockProvider,
            ["metaConfigured"] = report.MetaConfigured,
            ["liveMode"] = report.LiveMode,
            ["ready"] = report.Ready,
        };
}
