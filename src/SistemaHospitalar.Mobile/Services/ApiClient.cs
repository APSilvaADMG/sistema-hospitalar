using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SistemaHospitalar.Application.Serialization;

namespace SistemaHospitalar.Mobile.Services;

public class ApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new PortugueseEnumJsonConverterFactory(),
            new JsonStringEnumConverter(),
        },
    };

    private string? _token;

    public void SetBaseUrl(string baseUrl)
    {
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    }

    public void SetToken(string? token)
    {
        _token = token;
        http.DefaultRequestHeaders.Authorization = token is null
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<LoginResult?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/auth/login", new { email, password }, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResult>(JsonOptions, ct);
        if (result?.Token is not null)
        {
            SetToken(result.Token);
        }

        return result;
    }

    public async Task<SyncPullResult?> PullAsync(DateTime? since, CancellationToken ct = default)
    {
        var url = since.HasValue
            ? $"api/sync/pull?since={Uri.EscapeDataString(since.Value.ToUniversalTime().ToString("O"))}"
            : "api/sync/pull";

        var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SyncPullResult>(JsonOptions, ct);
    }

    public async Task<SyncPushResult?> PushAsync(string deviceId, IReadOnlyList<SyncMutationItem> mutations, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/sync/push", new { deviceId, mutations }, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SyncPushResult>(JsonOptions, ct);
    }
}

public record LoginResult(string Token, Guid UserId, string FullName, string Email, string Role);

public record SyncMutationItem(
    Guid ClientMutationId,
    string Entity,
    string Action,
    object Payload,
    DateTime ClientTimestamp);

public record SyncPushResult(DateTime ServerTimestamp, List<SyncMutationResult> Results);

public record SyncMutationResult(Guid ClientMutationId, string Status, string? Message);

public record SyncPullResult(
    DateTime ServerTimestamp,
    List<TransportDto> TransportRequests,
    List<PorterDto> Porters);

public record PorterDto(Guid Id, string FullName, string? JobTitle);

public record TransportDto(
    Guid Id,
    string PatientName,
    string OriginType,
    string? OriginDetail,
    string DestinationType,
    string? DestinationDetail,
    string Status,
    string Priority,
    string? AssignedEmployeeName,
    string? TransportAssetCode,
    DateTime RequestedAt,
    DateTime? AcceptedAt,
    DateTime? CompletedAt);
