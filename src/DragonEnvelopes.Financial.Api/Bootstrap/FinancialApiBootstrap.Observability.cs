using Asp.Versioning;
using DragonEnvelopes.Financial.Api.CrossCutting.OpenApi;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

namespace DragonEnvelopes.Financial.Api.Bootstrap;

internal static partial class FinancialApiBootstrap
{
    public static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            var enableLokiSink = context.Configuration.GetValue<bool>("Observability:EnableLokiSink");
            var lokiUrl = context.Configuration["Observability:LokiUrl"];

            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "DragonEnvelopes.Financial.Api")
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

            if (enableLokiSink && !string.IsNullOrWhiteSpace(lokiUrl))
            {
                configuration.WriteTo.GrafanaLoki(
                    lokiUrl,
                    labels:
                    [
                        new LokiLabel { Key = "application", Value = "dragonenvelopes-financial-api" },
                        new LokiLabel { Key = "environment", Value = context.HostingEnvironment.EnvironmentName.ToLowerInvariant() }
                    ],
                    propertiesAsLabels:
                    [
                        "CorrelationId",
                        "RequestPath",
                        "StatusCode",
                        "ExceptionType"
                    ]);
            }
        });
    }

    public static void ConfigureOpenApi(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DragonEnvelopes Financial API",
                Version = "v1",
                Description = "Financial integrations domain service APIs."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT bearer token. Example: `Bearer eyJhbGciOi...`"
            });

            options.OperationFilter<BearerSecurityOperationFilter>();
        });
    }

    public static void ConfigureApiVersioning(IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });
    }
}

