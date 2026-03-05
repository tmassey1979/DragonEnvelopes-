namespace DragonEnvelopes.Desktop.Api;

public sealed class ApiClientOptions
{
    public string BaseUrl { get; init; } =
        Environment.GetEnvironmentVariable("DRAGONENVELOPES_API_BASE_URL")
        ?? "http://localhost:18088/api/v1/";
}
