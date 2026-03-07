using DragonEnvelopes.Application;
using DragonEnvelopes.Infrastructure;
using DragonEnvelopes.Ledger.Api.CrossCutting.Errors;
using DragonEnvelopes.ProviderClients;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
}
