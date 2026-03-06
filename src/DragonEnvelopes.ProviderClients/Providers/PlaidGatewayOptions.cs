namespace DragonEnvelopes.ProviderClients.Providers;

public sealed class PlaidGatewayOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://sandbox.plaid.com";
    public string ClientId { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string ClientName { get; set; } = "DragonEnvelopes";
    public string Language { get; set; } = "en";
    public string[] CountryCodes { get; set; } = ["US"];
    public string[] Products { get; set; } = ["transactions"];
}
