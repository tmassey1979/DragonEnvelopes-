using System.Net.Http;
using System.Text.Json;
using DragonEnvelopes.Contracts.Runtime;
using DragonEnvelopes.Desktop.Api;

namespace DragonEnvelopes.Desktop.Services;

public sealed class SystemStatusDataService : ISystemStatusDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan ExternalProbeTimeout = TimeSpan.FromSeconds(2);
    private readonly IBackendApiClient _apiClient;
    private readonly string _familyApiHealthUrl;
    private readonly string _ledgerApiHealthUrl;
    private readonly string _financialApiHealthUrl;

    public SystemStatusDataService(IBackendApiClient apiClient)
    {
        _apiClient = apiClient;
        _familyApiHealthUrl = ResolveHealthUrl(
            "DRAGONENVELOPES_FAMILY_API_HEALTH_URL",
            "http://localhost:18089/health/ready");
        _ledgerApiHealthUrl = ResolveHealthUrl(
            "DRAGONENVELOPES_LEDGER_API_HEALTH_URL",
            "http://localhost:18090/health/ready");
        _financialApiHealthUrl = ResolveHealthUrl(
            "DRAGONENVELOPES_FINANCIAL_API_HEALTH_URL",
            "http://localhost:18091/health/ready");
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

        var familyProbe = await ProbeExternalServiceHealthAsync("Family API", _familyApiHealthUrl, cancellationToken);
        var ledgerProbe = await ProbeExternalServiceHealthAsync("Ledger API", _ledgerApiHealthUrl, cancellationToken);
        var financialProbe = await ProbeExternalServiceHealthAsync("Financial API", _financialApiHealthUrl, cancellationToken);

        var checkedAt = health.UtcTime > version.UtcTime
            ? health.UtcTime
            : version.UtcTime;

        return new SystemRuntimeStatusData(
            health.Status,
            version.Version,
            version.Environment,
            checkedAt,
            familyProbe.Status,
            ledgerProbe.Status,
            familyProbe.Message,
            ledgerProbe.Message,
            financialProbe.Status,
            financialProbe.Message);
    }

    private async Task<ServiceProbeResult> ProbeExternalServiceHealthAsync(
        string serviceName,
        string healthUrl,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient
        {
            Timeout = ExternalProbeTimeout
        };

        try
        {
            using var response = await client.GetAsync(healthUrl, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new ServiceProbeResult("Healthy", $"{serviceName} reachable ({healthUrl}).");
            }

            return new ServiceProbeResult(
                "Unavailable",
                $"{serviceName} returned status {(int)response.StatusCode} ({healthUrl}).");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ServiceProbeResult("Unavailable", $"{serviceName} timed out ({healthUrl}).");
        }
        catch (HttpRequestException ex)
        {
            return new ServiceProbeResult("Unavailable", $"{serviceName} probe failed: {ex.Message}");
        }
        catch (UriFormatException ex)
        {
            return new ServiceProbeResult("Unavailable", $"{serviceName} health URL is invalid: {ex.Message}");
        }
    }

    private static string ResolveHealthUrl(string environmentVariable, string fallback)
    {
        var configured = Environment.GetEnvironmentVariable(environmentVariable);
        return string.IsNullOrWhiteSpace(configured)
            ? fallback
            : configured.Trim();
    }

    private readonly record struct ServiceProbeResult(string Status, string Message);
}
