using DragonEnvelopes.Application;
using DragonEnvelopes.Family.Api.CrossCutting.Errors;
using DragonEnvelopes.Family.Api.Services;
using DragonEnvelopes.Infrastructure;
using DragonEnvelopes.ProviderClients;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Family.Api.Bootstrap;

internal static partial class FamilyApiBootstrap
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
        builder.Services.AddSingleton(BuildKeycloakAdminOptions(builder.Configuration));
        builder.Services.AddHttpClient<IKeycloakProvisioningService, KeycloakProvisioningService>();
        builder.Services.AddSingleton(Options.Create(BuildOutboxWorkerOptions(builder.Configuration)));

        var enableRabbitMq = builder.Configuration.GetValue<bool>("Messaging:RabbitMq:Enabled");
        var outboxWorkerEnabled = builder.Configuration.GetValue<bool>("Messaging:Outbox:Enabled", true);
        if (!builder.Environment.IsEnvironment("Testing") && enableRabbitMq && outboxWorkerEnabled)
        {
            builder.Services.AddHostedService<FamilyOutboxDispatchWorker>();
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

    private static FamilyOutboxDispatchWorkerOptions BuildOutboxWorkerOptions(IConfiguration configuration)
    {
        return new FamilyOutboxDispatchWorkerOptions
        {
            Enabled = !bool.TryParse(configuration["Messaging:Outbox:Enabled"], out var enabled) || enabled,
            PollIntervalSeconds = int.TryParse(configuration["Messaging:Outbox:PollIntervalSeconds"], out var pollIntervalSeconds)
                ? Math.Max(1, pollIntervalSeconds)
                : 5,
            BatchSize = int.TryParse(configuration["Messaging:Outbox:BatchSize"], out var batchSize)
                ? Math.Clamp(batchSize, 1, 500)
                : 50,
            BacklogWarningThreshold = int.TryParse(configuration["Messaging:Outbox:BacklogWarningThreshold"], out var warningThreshold)
                ? Math.Max(1, warningThreshold)
                : 100
        };
    }
}
