using DragonEnvelopes.Application.Services;
using DragonEnvelopes.ProviderClients.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.ProviderClients;

public static class DependencyInjection
{
    public static IServiceCollection AddProviderClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var plaidGatewayOptions = new PlaidGatewayOptions
        {
            Enabled = bool.TryParse(configuration["Plaid:Enabled"], out var plaidEnabled) && plaidEnabled,
            BaseUrl = configuration["Plaid:BaseUrl"] ?? "https://sandbox.plaid.com",
            ClientId = configuration["Plaid:ClientId"] ?? string.Empty,
            Secret = configuration["Plaid:Secret"] ?? string.Empty,
            ClientName = configuration["Plaid:ClientName"] ?? "DragonEnvelopes",
            Language = configuration["Plaid:Language"] ?? "en",
            CountryCodes = configuration.GetSection("Plaid:CountryCodes").Get<string[]>() ?? ["US"],
            Products = configuration.GetSection("Plaid:Products").Get<string[]>() ?? ["transactions"]
        };
        var stripeGatewayOptions = new StripeGatewayOptions
        {
            Enabled = bool.TryParse(configuration["Stripe:Enabled"], out var stripeEnabled) && stripeEnabled,
            ApiBaseUrl = configuration["Stripe:ApiBaseUrl"] ?? "https://api.stripe.com",
            SecretKey = configuration["Stripe:SecretKey"] ?? string.Empty
        };

        services.AddSingleton(Options.Create(plaidGatewayOptions));
        services.AddSingleton(Options.Create(stripeGatewayOptions));
        services.AddHttpClient<IPlaidGateway, PlaidGateway>();
        services.AddHttpClient<IStripeGateway, StripeGateway>();

        return services;
    }
}
