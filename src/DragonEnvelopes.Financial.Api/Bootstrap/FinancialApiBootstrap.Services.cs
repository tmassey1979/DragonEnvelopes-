using DragonEnvelopes.Application;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Financial.Api.CrossCutting.Errors;
using DragonEnvelopes.Financial.Api.Services;
using DragonEnvelopes.Infrastructure;
using DragonEnvelopes.ProviderClients;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Financial.Api.Bootstrap;

internal static partial class FinancialApiBootstrap
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
        builder.Services.AddSingleton(Options.Create(BuildStripeWebhookOptions(builder.Configuration)));
        builder.Services.AddSingleton(Options.Create(BuildDataRetentionOptions(builder.Configuration)));

        var enableFinancialWorkers = builder.Configuration.GetValue<bool>("FinancialWorkers:Enabled");
        var enableLedgerTransactionConsumer =
            builder.Configuration.GetValue<bool>("Messaging:RabbitMq:Enabled")
            && builder.Configuration.GetValue<bool>("Messaging:RabbitMq:EnableLedgerTransactionConsumer");

        if (!builder.Environment.IsEnvironment("Testing") && enableLedgerTransactionConsumer)
        {
            builder.Services.AddHostedService<LedgerTransactionCreatedConsumer>();
        }

        if (!builder.Environment.IsEnvironment("Testing") && enableFinancialWorkers)
        {
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

