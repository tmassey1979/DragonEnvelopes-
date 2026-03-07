namespace DragonEnvelopes.Desktop.Api;

public sealed class ApiClientOptions
{
    public string BaseUrl { get; init; } =
        Environment.GetEnvironmentVariable("DRAGONENVELOPES_API_BASE_URL")
        ?? "http://localhost:18088/api/v1/";

    public string? FamilyBaseUrl { get; init; } =
        Environment.GetEnvironmentVariable("DRAGONENVELOPES_FAMILY_API_BASE_URL");

    public string? LedgerBaseUrl { get; init; } =
        Environment.GetEnvironmentVariable("DRAGONENVELOPES_LEDGER_API_BASE_URL");

    public string ResolveFamilyBaseUrl()
    {
        return ResolveBaseUrl(FamilyBaseUrl, BaseUrl);
    }

    public string ResolveLedgerBaseUrl()
    {
        return ResolveBaseUrl(LedgerBaseUrl, BaseUrl);
    }

    private static string ResolveBaseUrl(string? candidate, string fallback)
    {
        var resolved = string.IsNullOrWhiteSpace(candidate)
            ? fallback
            : candidate;
        return EnsureTrailingSlash(resolved);
    }

    private static string EnsureTrailingSlash(string value)
    {
        var trimmed = value.Trim();
        return trimmed.EndsWith("/", StringComparison.Ordinal) ? trimmed : $"{trimmed}/";
    }
}
