using System.Security.Claims;
using Asp.Versioning;
using DragonEnvelopes.Application;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Api.CrossCutting.Errors;
using DragonEnvelopes.Api.CrossCutting.Logging;
using DragonEnvelopes.Api.CrossCutting.OpenApi;
using DragonEnvelopes.Api.CrossCutting.Validation;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Families;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using DragonEnvelopes.Infrastructure;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);
var authority = builder.Configuration["Authentication:Authority"]
    ?? throw new InvalidOperationException("Authentication:Authority must be configured.");
var audience = builder.Configuration["Authentication:Audience"]
    ?? throw new InvalidOperationException("Authentication:Audience must be configured.");

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

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
            ValidateAudience = true,
            ValidAudience = audience,
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
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
    dbContext.Database.Migrate();

    logger.LogInformation("EF Core migrations applied successfully.");
}

// Configure the HTTP request pipeline.
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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

var v1 = app.MapGroup("/api/v{version:apiVersion}")
    .WithApiVersionSet(versionSet)
    .MapToApiVersion(new ApiVersion(1, 0))
    .AddFluentValidation();

v1.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.AllowAnonymous()
.WithName("GetWeatherForecast")
.WithOpenApi();

v1.MapGet("/auth/me", (ClaimsPrincipal user) =>
    {
        var roles = user.FindAll(ClaimTypes.Role)
            .Select(static claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static role => role)
            .ToArray();

        return Results.Ok(new
        {
            username = user.Identity?.Name,
            roles
        });
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("GetCurrentUser")
    .WithOpenApi();

v1.MapGet("/auth/parent-only", () =>
    Results.Ok(new { message = "Parent access granted." }))
    .RequireAuthorization(ApiAuthorizationPolicies.Parent)
    .WithName("ParentOnlyProbe")
    .WithOpenApi();

v1.MapPost("/families", async (
        CreateFamilyRequest request,
        IFamilyService familyService,
        CancellationToken cancellationToken) =>
    {
        var family = await familyService.CreateAsync(request.Name, cancellationToken);
        return Results.Created($"/api/v1/families/{family.Id}", MapFamilyResponse(family));
    })
    .RequireAuthorization(ApiAuthorizationPolicies.ParentOrAdult)
    .WithName("CreateFamily")
    .WithOpenApi();

v1.MapGet("/families/{familyId:guid}", async (
        Guid familyId,
        IFamilyService familyService,
        CancellationToken cancellationToken) =>
    {
        var family = await familyService.GetByIdAsync(familyId, cancellationToken);
        return family is null
            ? Results.NotFound()
            : Results.Ok(MapFamilyResponse(family));
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("GetFamilyById")
    .WithOpenApi();

app.Run();

static FamilyResponse MapFamilyResponse(FamilyDetails family)
{
    return new FamilyResponse(
        family.Id,
        family.Name,
        family.CreatedAt,
        family.Members
            .Select(static member => new FamilyMemberResponse(
                member.Id,
                member.FamilyId,
                member.KeycloakUserId,
                member.Name,
                member.Email,
                member.Role))
            .ToArray());
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
