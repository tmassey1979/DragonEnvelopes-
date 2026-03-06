using System.Security.Claims;
using Asp.Versioning;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Api.CrossCutting.Errors;
using DragonEnvelopes.Api.CrossCutting.Logging;
using DragonEnvelopes.Api.CrossCutting.OpenApi;
using DragonEnvelopes.Api.CrossCutting.Validation;
using DragonEnvelopes.Api.Endpoints;
using DragonEnvelopes.Api.Services;
using DragonEnvelopes.Application;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Infrastructure;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.ProviderClients;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);
var authority = builder.Configuration["Authentication:Authority"]
    ?? throw new InvalidOperationException("Authentication:Authority must be configured.");
var audience = builder.Configuration["Authentication:Audience"]
    ?? throw new InvalidOperationException("Authentication:Audience must be configured.");
var publicAuthority = builder.Configuration["Authentication:PublicAuthority"];
var allowedAuthorizedParties = builder.Configuration
    .GetSection("Authentication:AllowedAuthorizedParties")
    .Get<string[]>()
    ?? [audience, "dragonenvelopes-desktop"];
var validIssuers = BuildValidIssuers(authority, publicAuthority);

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
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

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.Authority = authority;
        options.Audience = audience;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidIssuers = validIssuers,
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal is null
                    || !IsTokenIntendedForApi(context.Principal, audience, allowedAuthorizedParties))
                {
                    context.Fail("Token audience/authorized party is not allowed for this API.");
                    return Task.CompletedTask;
                }

                if (context.Principal is not null)
                {
                    KeycloakRoleClaimsTransformer.AddRoleClaims(context.Principal, audience);
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ApiAuthorizationPolicies.Parent, policy => policy.RequireRole("Parent"));
    options.AddPolicy(ApiAuthorizationPolicies.Adult, policy => policy.RequireRole("Adult"));
    options.AddPolicy(ApiAuthorizationPolicies.Teen, policy => policy.RequireRole("Teen"));
    options.AddPolicy(ApiAuthorizationPolicies.Child, policy => policy.RequireRole("Child"));
    options.AddPolicy(ApiAuthorizationPolicies.ParentOrAdult, policy => policy.RequireRole("Parent", "Adult"));
    options.AddPolicy(ApiAuthorizationPolicies.TeenOrAbove, policy => policy.RequireRole("Parent", "Adult", "Teen"));
    options.AddPolicy(ApiAuthorizationPolicies.AnyFamilyMember, policy => policy.RequireRole(ApiAuthorizationPolicies.FamilyRoles));
});

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

var defaultConnection = builder.Configuration.GetConnectionString("Default");
var healthChecks = builder.Services.AddHealthChecks()
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("StartupMigrations");

    logger.LogInformation("Applying EF Core migrations at startup.");

    var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
        logger.LogInformation("EF Core migrations applied successfully.");
    }
    else
    {
        dbContext.Database.EnsureCreated();
        logger.LogInformation("Ensured database created for non-relational provider.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
        diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value);
        diagnosticContext.Set("StatusCode", httpContext.Response.StatusCode);
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
    };
});

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
})
.AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
})
.AllowAnonymous();

var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

var v1 = app.MapGroup("/api/v{version:apiVersion}")
    .WithApiVersionSet(versionSet)
    .MapToApiVersion(new ApiVersion(1, 0))
    .AddFluentValidation();

string[] summaries =
[
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
];

v1.MapSystemAndAuthEndpoints(summaries)
    .MapFamilyEndpoints()
    .MapFinancialIntegrationEndpoints()
    .MapAutomationEndpoints()
    .MapAccountAndTransactionEndpoints()
    .MapPlanningAndReportingEndpoints();

app.Run();

static KeycloakAdminOptions BuildKeycloakAdminOptions(IConfiguration configuration)
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

static StripeWebhookOptions BuildStripeWebhookOptions(IConfiguration configuration)
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

static DataRetentionOptions BuildDataRetentionOptions(IConfiguration configuration)
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

static bool IsTokenIntendedForApi(
    ClaimsPrincipal principal,
    string requiredAudience,
    IReadOnlyCollection<string> allowedAuthorizedParties)
{
    var audiences = principal.FindAll("aud")
        .Select(static claim => claim.Value)
        .Where(static value => !string.IsNullOrWhiteSpace(value))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    if (audiences.Contains(requiredAudience))
    {
        return true;
    }

    var authorizedParty = principal.FindFirst("azp")?.Value;
    return !string.IsNullOrWhiteSpace(authorizedParty)
        && allowedAuthorizedParties.Contains(authorizedParty, StringComparer.OrdinalIgnoreCase);
}

static string[] BuildValidIssuers(string authority, string? publicAuthority)
{
    static string NormalizeIssuer(string value) => value.TrimEnd('/');

    var issuers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        NormalizeIssuer(authority)
    };

    if (!string.IsNullOrWhiteSpace(publicAuthority))
    {
        issuers.Add(NormalizeIssuer(publicAuthority));
    }

    return issuers.ToArray();
}

public partial class Program
{
}
