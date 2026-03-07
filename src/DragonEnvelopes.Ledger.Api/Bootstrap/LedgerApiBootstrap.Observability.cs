using Asp.Versioning;
using DragonEnvelopes.Ledger.Api.CrossCutting.OpenApi;
using Microsoft.OpenApi.Models;
using Serilog;

namespace DragonEnvelopes.Ledger.Api.Bootstrap;

internal static partial class LedgerApiBootstrap
{
    public static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "DragonEnvelopes.Ledger.Api")
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);
        });
    }

    public static void ConfigureOpenApi(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DragonEnvelopes Ledger API",
                Version = "v1",
                Description = "Ledger domain service APIs."
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
