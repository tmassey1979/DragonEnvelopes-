namespace DragonEnvelopes.ProviderClients.Providers;

public sealed class StripeGatewayOptions
{
    public bool Enabled { get; set; }
    public string ApiBaseUrl { get; set; } = "https://api.stripe.com";
    public string SecretKey { get; set; } = string.Empty;
}
