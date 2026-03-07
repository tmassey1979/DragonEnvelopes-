using DragonEnvelopes.Application;
using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Infrastructure;
using DragonEnvelopes.Ledger.Api.CrossCutting.Errors;
using DragonEnvelopes.Ledger.Api.Services;
using DragonEnvelopes.ProviderClients;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Ledger.Api.Bootstrap;

internal static partial class LedgerApiBootstrap
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
        builder.Services.AddSingleton(Options.Create(BuildSpendAnomalyDetectionOptions(builder.Configuration)));
        builder.Services.AddSingleton(Options.Create(BuildOutboxWorkerOptions(builder.Configuration)));

        var enableRabbitMq = builder.Configuration.GetValue<bool>("Messaging:RabbitMq:Enabled");
        var outboxWorkerEnabled = builder.Configuration.GetValue<bool>("Messaging:Outbox:Enabled", true);
        if (!builder.Environment.IsEnvironment("Testing") && enableRabbitMq && outboxWorkerEnabled)
        {
            builder.Services.AddHostedService<LedgerOutboxDispatchWorker>();
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

    private static SpendAnomalyDetectionOptions BuildSpendAnomalyDetectionOptions(IConfiguration configuration)
    {
        return new SpendAnomalyDetectionOptions
        {
            LookbackDays = int.TryParse(configuration["SpendAnomalies:LookbackDays"], out var lookbackDays)
                ? Math.Max(1, lookbackDays)
                : 90,
            HistorySampleLimit = int.TryParse(configuration["SpendAnomalies:HistorySampleLimit"], out var sampleLimit)
                ? Math.Max(10, sampleLimit)
                : 500,
            MinimumMerchantSamples = int.TryParse(configuration["SpendAnomalies:MinimumMerchantSamples"], out var minimumMerchantSamples)
                ? Math.Max(1, minimumMerchantSamples)
                : 3,
            MinimumFamilySamples = int.TryParse(configuration["SpendAnomalies:MinimumFamilySamples"], out var minimumFamilySamples)
                ? Math.Max(1, minimumFamilySamples)
                : 10,
            MerchantDeviationZScoreThreshold = decimal.TryParse(
                configuration["SpendAnomalies:MerchantDeviationZScoreThreshold"],
                out var merchantDeviationThreshold)
                ? Math.Max(0.1m, merchantDeviationThreshold)
                : 2.5m,
            FamilyDeviationRatioThreshold = decimal.TryParse(
                configuration["SpendAnomalies:FamilyDeviationRatioThreshold"],
                out var familyDeviationRatioThreshold)
                ? Math.Max(1m, familyDeviationRatioThreshold)
                : 3.0m,
            MinimumAbsoluteAmount = decimal.TryParse(configuration["SpendAnomalies:MinimumAbsoluteAmount"], out var minimumAbsoluteAmount)
                ? Math.Max(0m, minimumAbsoluteAmount)
                : 50m,
            MinimumStandardDeviation = decimal.TryParse(configuration["SpendAnomalies:MinimumStandardDeviation"], out var minimumStandardDeviation)
                ? Math.Max(0.01m, minimumStandardDeviation)
                : 5m,
            MaxListTake = int.TryParse(configuration["SpendAnomalies:MaxListTake"], out var maxListTake)
                ? Math.Max(1, maxListTake)
                : 200
        };
    }

    private static LedgerOutboxDispatchWorkerOptions BuildOutboxWorkerOptions(IConfiguration configuration)
    {
        var sourceServices = configuration
            .GetSection("Messaging:Outbox:SourceServices")
            .Get<string[]>();

        return new LedgerOutboxDispatchWorkerOptions
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
                : 100,
            SourceServices = sourceServices is { Length: > 0 }
                ? sourceServices
                : [IntegrationEventSourceServices.LedgerApi, IntegrationEventSourceServices.PlanningApi, IntegrationEventSourceServices.AutomationApi]
        };
    }
}

