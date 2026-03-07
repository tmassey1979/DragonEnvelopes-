using Asp.Versioning;
using DragonEnvelopes.Api.CrossCutting.OpenApi;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

namespace DragonEnvelopes.Api.Bootstrap;

internal static partial class ApiBootstrap
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
                .Enrich.WithProperty("Application", "DragonEnvelopes.Api")
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

            if (enableLokiSink && !string.IsNullOrWhiteSpace(lokiUrl))
            {
                configuration.WriteTo.GrafanaLoki(
                    lokiUrl,
                    labels:
                    [
                        new LokiLabel { Key = "application", Value = "dragonenvelopes-api" },
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
                Title = "DragonEnvelopes API",
                Version = "v1",
                Description = "Versioning strategy: URL segment `/api/v{version}`. Current stable version: v1."
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
