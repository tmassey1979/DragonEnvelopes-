using DragonEnvelopes.Api.CrossCutting.Errors;
using DragonEnvelopes.Api.Services;
using DragonEnvelopes.Application;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Infrastructure;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using DragonEnvelopes.ProviderClients;

namespace DragonEnvelopes.Api.Bootstrap;

internal static partial class ApiBootstrap
{
    public static void ConfigureDependencyInjection(WebApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped);
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance = context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            };
        });
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddProviderClients(builder.Configuration);
        builder.Services.AddScoped<IRecurringAutoPostService, RecurringAutoPostService>();
        builder.Services.AddSingleton(Options.Create(BuildStripeWebhookOptions(builder.Configuration)));
        builder.Services.AddSingleton(Options.Create(BuildDataRetentionOptions(builder.Configuration)));
        builder.Services.AddSingleton(BuildKeycloakAdminOptions(builder.Configuration));
        builder.Services.AddHttpClient<IKeycloakProvisioningService, KeycloakProvisioningService>();
        if (!builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddHostedService<RecurringBillAutoPostWorker>();
            builder.Services.AddHostedService<SpendNotificationDispatchWorker>();
            builder.Services.AddHostedService<DataRetentionWorker>();
            builder.Services.AddHostedService<PlaidTransactionSyncWorker>();
            builder.Services.AddHostedService<PlaidBalanceRefreshWorker>();
        }
    }

    public static void ConfigureHealthChecks(IServiceCollection services, string? defaultConnection)
    {
        var healthChecks = services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        if (!string.IsNullOrWhiteSpace(defaultConnection))
        {
            healthChecks.AddNpgSql(defaultConnection, name: "postgres", tags: ["ready"]);
        }
        else
        {
            healthChecks.AddCheck(
                "postgres-connection-string",
                () => HealthCheckResult.Unhealthy("ConnectionStrings:Default is not configured."),
                tags: ["ready"]);
        }
    }


    private static KeycloakAdminOptions BuildKeycloakAdminOptions(IConfiguration configuration)
    {
        var defaults = new KeycloakAdminOptions();
        return new KeycloakAdminOptions
        {
            ServerUrl = configuration["Keycloak:ServerUrl"] ?? defaults.ServerUrl,
            Realm = configuration["Keycloak:Realm"] ?? defaults.Realm,
            AdminRealm = configuration["Keycloak:AdminRealm"] ?? defaults.AdminRealm,
            AdminClientId = configuration["Keycloak:AdminClientId"] ?? defaults.AdminClientId,
            AdminUsername = configuration["Keycloak:AdminUsername"] ?? defaults.AdminUsername,
            AdminPassword = configuration["Keycloak:AdminPassword"] ?? defaults.AdminPassword
        };
    }

    private static StripeWebhookOptions BuildStripeWebhookOptions(IConfiguration configuration)
    {
        return new StripeWebhookOptions
        {
            Enabled = bool.TryParse(configuration["Stripe:Webhooks:Enabled"], out var enabled) && enabled,
            SigningSecret = configuration["Stripe:Webhooks:SigningSecret"] ?? string.Empty,
            SignatureToleranceSeconds = int.TryParse(configuration["Stripe:Webhooks:SignatureToleranceSeconds"], out var tolerance)
                ? Math.Max(1, tolerance)
                : 300
        };
    }

    private static DataRetentionOptions BuildDataRetentionOptions(IConfiguration configuration)
    {
        return new DataRetentionOptions
        {
            Enabled = !bool.TryParse(configuration["DataRetention:Enabled"], out var enabled) || enabled,
            PollIntervalMinutes = int.TryParse(configuration["DataRetention:PollIntervalMinutes"], out var intervalMinutes)
                ? Math.Max(5, intervalMinutes)
                : 720,
            BatchSize = int.TryParse(configuration["DataRetention:BatchSize"], out var batchSize)
                ? Math.Max(1, batchSize)
                : 500,
            StripeWebhookRetentionDays = int.TryParse(configuration["DataRetention:StripeWebhookRetentionDays"], out var stripeDays)
                ? Math.Max(1, stripeDays)
                : 90,
            SpendNotificationRetentionDays = int.TryParse(configuration["DataRetention:SpendNotificationRetentionDays"], out var notificationDays)
                ? Math.Max(1, notificationDays)
                : 90
        };
    }
}
