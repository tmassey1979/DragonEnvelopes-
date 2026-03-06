using System.Text.Json;
using DragonEnvelopes.Contracts.Runtime;
using DragonEnvelopes.Desktop.Api;

namespace DragonEnvelopes.Desktop.Services;

public sealed class SystemStatusDataService : ISystemStatusDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;

    public SystemStatusDataService(IBackendApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<SystemRuntimeStatusData> GetRuntimeStatusAsync(CancellationToken cancellationToken = default)
    {
        using var healthResponse = await _apiClient.GetAsync("system/health", cancellationToken);
        if (!healthResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"System health request failed with status {(int)healthResponse.StatusCode}.");
        }

        await using var healthStream = await healthResponse.Content.ReadAsStreamAsync(cancellationToken);
        var health = await JsonSerializer.DeserializeAsync<ApiHealthResponse>(healthStream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("System health response payload was invalid.");

        using var versionResponse = await _apiClient.GetAsync("system/version", cancellationToken);
        if (!versionResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"System version request failed with status {(int)versionResponse.StatusCode}.");
        }

        await using var versionStream = await versionResponse.Content.ReadAsStreamAsync(cancellationToken);
        var version = await JsonSerializer.DeserializeAsync<ApiVersionResponse>(versionStream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("System version response payload was invalid.");

        var checkedAt = health.UtcTime > version.UtcTime
            ? health.UtcTime
            : version.UtcTime;

        return new SystemRuntimeStatusData(
            health.Status,
            version.Version,
            version.Environment,
            checkedAt);
    }
}
