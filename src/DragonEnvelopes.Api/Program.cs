using System.Security.Claims;
using Asp.Versioning;
using DragonEnvelopes.Application;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Api.CrossCutting.Errors;
using DragonEnvelopes.Api.CrossCutting.Logging;
using DragonEnvelopes.Api.CrossCutting.OpenApi;
using DragonEnvelopes.Api.CrossCutting.Validation;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Api.Services;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Accounts;
using DragonEnvelopes.Contracts.Budgets;
using DragonEnvelopes.Contracts.Envelopes;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Contracts.Reports;
using DragonEnvelopes.Contracts.Transactions;
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
builder.Services.AddSingleton(BuildKeycloakAdminOptions(builder.Configuration));
builder.Services.AddHttpClient<IKeycloakProvisioningService, KeycloakProvisioningService>();

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
    .AllowAnonymous()
    .WithName("CreateFamily")
    .WithOpenApi();

v1.MapPost("/families/onboard", async (
        CompleteFamilyOnboardingRequest request,
        IFamilyService familyService,
        IKeycloakProvisioningService keycloakProvisioningService,
        CancellationToken cancellationToken) =>
    {
        var keycloakUserId = await keycloakProvisioningService.CreateUserAsync(
            request.Email,
            request.PrimaryGuardianFirstName,
            request.PrimaryGuardianLastName,
            request.Password,
            cancellationToken);
        await keycloakProvisioningService.AssignRealmRoleAsync(
            keycloakUserId,
            "Parent",
            cancellationToken);

        try
        {
            var family = await familyService.CreateAsync(request.FamilyName, cancellationToken);
            var guardianDisplayName = $"{request.PrimaryGuardianFirstName} {request.PrimaryGuardianLastName}".Trim();
            await familyService.AddMemberAsync(
                family.Id,
                keycloakUserId,
                guardianDisplayName,
                request.Email,
                "Parent",
                cancellationToken);

            return Results.Created($"/api/v1/families/{family.Id}", MapFamilyResponse(family));
        }
        catch
        {
            try
            {
                await keycloakProvisioningService.DeleteUserAsync(keycloakUserId, cancellationToken);
            }
            catch
            {
                // Best-effort compensation to avoid orphaned identity users.
            }

            throw;
        }
    })
    .AllowAnonymous()
    .WithName("CompleteFamilyOnboarding")
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

v1.MapPost("/families/{familyId:guid}/members", async (
        Guid familyId,
        AddFamilyMemberRequest request,
        IFamilyService familyService,
        CancellationToken cancellationToken) =>
    {
        var member = await familyService.AddMemberAsync(
            familyId,
            request.KeycloakUserId,
            request.Name,
            request.Email,
            request.Role,
            cancellationToken);

        return Results.Created(
            $"/api/v1/families/{familyId}/members/{member.Id}",
            MapFamilyMemberResponse(member));
    })
    .AllowAnonymous()
    .WithName("AddFamilyMember")
    .WithOpenApi();

v1.MapGet("/families/{familyId:guid}/members", async (
        Guid familyId,
        IFamilyService familyService,
        CancellationToken cancellationToken) =>
    {
        var members = await familyService.ListMembersAsync(familyId, cancellationToken);
        return members is null
            ? Results.NotFound()
            : Results.Ok(members.Select(MapFamilyMemberResponse).ToArray());
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("ListFamilyMembers")
    .WithOpenApi();

v1.MapPost("/accounts", async (
        CreateAccountRequest request,
        IAccountService accountService,
        CancellationToken cancellationToken) =>
    {
        var account = await accountService.CreateAsync(
            request.FamilyId,
            request.Name,
            request.Type,
            request.OpeningBalance,
            cancellationToken);

        return Results.Created($"/api/v1/accounts/{account.Id}", MapAccountResponse(account));
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("CreateAccount")
    .WithOpenApi();

v1.MapGet("/accounts", async (
        Guid? familyId,
        IAccountService accountService,
        CancellationToken cancellationToken) =>
    {
        var accounts = await accountService.ListAsync(familyId, cancellationToken);
        return Results.Ok(accounts.Select(MapAccountResponse).ToArray());
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("ListAccounts")
    .WithOpenApi();

v1.MapPost("/transactions", async (
        CreateTransactionRequest request,
        ITransactionService transactionService,
        CancellationToken cancellationToken) =>
    {
        var transaction = await transactionService.CreateAsync(
            request.AccountId,
            request.Amount,
            request.Description,
            request.Merchant,
            request.OccurredAt,
            request.Category,
            request.EnvelopeId,
            request.Splits is { Count: > 0 },
            request.Splits?
                .Select(static split => new TransactionSplitCreateDetails(
                    split.EnvelopeId,
                    split.Amount,
                    split.Category,
                    split.Notes))
                .ToArray(),
            cancellationToken);
        return Results.Created($"/api/v1/transactions/{transaction.Id}", MapTransactionResponse(transaction));
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("CreateTransaction")
    .WithOpenApi();

v1.MapGet("/transactions", async (
        Guid? accountId,
        ITransactionService transactionService,
        CancellationToken cancellationToken) =>
    {
        var transactions = await transactionService.ListAsync(accountId, cancellationToken);
        return Results.Ok(transactions.Select(MapTransactionResponse).ToArray());
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("ListTransactions")
    .WithOpenApi();

v1.MapPost("/envelopes", async (
        CreateEnvelopeRequest request,
        IEnvelopeService envelopeService,
        CancellationToken cancellationToken) =>
    {
        var envelope = await envelopeService.CreateAsync(
            request.FamilyId,
            request.Name,
            request.MonthlyBudget,
            cancellationToken);
        return Results.Created($"/api/v1/envelopes/{envelope.Id}", MapEnvelopeResponse(envelope));
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("CreateEnvelope")
    .WithOpenApi();

v1.MapGet("/envelopes/{envelopeId:guid}", async (
        Guid envelopeId,
        IEnvelopeService envelopeService,
        CancellationToken cancellationToken) =>
    {
        var envelope = await envelopeService.GetByIdAsync(envelopeId, cancellationToken);
        return envelope is null
            ? Results.NotFound()
            : Results.Ok(MapEnvelopeResponse(envelope));
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("GetEnvelopeById")
    .WithOpenApi();

v1.MapGet("/envelopes", async (
        Guid familyId,
        IEnvelopeService envelopeService,
        CancellationToken cancellationToken) =>
    {
        var envelopes = await envelopeService.ListByFamilyAsync(familyId, cancellationToken);
        return Results.Ok(envelopes.Select(MapEnvelopeResponse).ToArray());
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("ListEnvelopes")
    .WithOpenApi();

v1.MapPut("/envelopes/{envelopeId:guid}", async (
        Guid envelopeId,
        UpdateEnvelopeRequest request,
        IEnvelopeService envelopeService,
        CancellationToken cancellationToken) =>
    {
        var envelope = await envelopeService.UpdateAsync(
            envelopeId,
            request.Name,
            request.MonthlyBudget,
            request.IsArchived,
            cancellationToken);
        return Results.Ok(MapEnvelopeResponse(envelope));
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("UpdateEnvelope")
    .WithOpenApi();

v1.MapPost("/envelopes/{envelopeId:guid}/archive", async (
        Guid envelopeId,
        IEnvelopeService envelopeService,
        CancellationToken cancellationToken) =>
    {
        var envelope = await envelopeService.ArchiveAsync(envelopeId, cancellationToken);
        return Results.Ok(MapEnvelopeResponse(envelope));
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("ArchiveEnvelope")
    .WithOpenApi();

v1.MapPost("/budgets", async (
        CreateBudgetRequest request,
        IBudgetService budgetService,
        CancellationToken cancellationToken) =>
    {
        var budget = await budgetService.CreateAsync(
            request.FamilyId,
            request.Month,
            request.TotalIncome,
            cancellationToken);
        return Results.Created($"/api/v1/budgets/{budget.Id}", MapBudgetResponse(budget));
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("CreateBudget")
    .WithOpenApi();

v1.MapGet("/budgets/{familyId:guid}/{month}", async (
        Guid familyId,
        string month,
        IBudgetService budgetService,
        CancellationToken cancellationToken) =>
    {
        var budget = await budgetService.GetByMonthAsync(familyId, month, cancellationToken);
        return budget is null
            ? Results.NotFound()
            : Results.Ok(MapBudgetResponse(budget));
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("GetBudgetByMonth")
    .WithOpenApi();

v1.MapPut("/budgets/{budgetId:guid}", async (
        Guid budgetId,
        UpdateBudgetRequest request,
        IBudgetService budgetService,
        CancellationToken cancellationToken) =>
    {
        var budget = await budgetService.UpdateAsync(
            budgetId,
            request.TotalIncome,
            cancellationToken);
        return Results.Ok(MapBudgetResponse(budget));
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("UpdateBudget")
    .WithOpenApi();

v1.MapGet("/reports/envelope-balances", async (
        Guid familyId,
        IReportingService reportingService,
        CancellationToken cancellationToken) =>
    {
        var result = await reportingService.GetEnvelopeBalancesAsync(familyId, cancellationToken);
        return Results.Ok(result.Select(MapEnvelopeBalanceReportResponse).ToArray());
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("GetEnvelopeBalancesReport")
    .WithOpenApi();

v1.MapGet("/reports/monthly-spend", async (
        Guid familyId,
        DateTimeOffset from,
        DateTimeOffset to,
        IReportingService reportingService,
        CancellationToken cancellationToken) =>
    {
        var result = await reportingService.GetMonthlySpendAsync(familyId, from, to, cancellationToken);
        return Results.Ok(result.Select(MapMonthlySpendReportPointResponse).ToArray());
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("GetMonthlySpendReport")
    .WithOpenApi();

v1.MapGet("/reports/category-breakdown", async (
        Guid familyId,
        DateTimeOffset from,
        DateTimeOffset to,
        IReportingService reportingService,
        CancellationToken cancellationToken) =>
    {
        var result = await reportingService.GetCategoryBreakdownAsync(familyId, from, to, cancellationToken);
        return Results.Ok(result.Select(MapCategoryBreakdownReportItemResponse).ToArray());
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("GetCategoryBreakdownReport")
    .WithOpenApi();

v1.MapGet("/reports/remaining-budget", async (
        Guid familyId,
        string month,
        IReportingService reportingService,
        CancellationToken cancellationToken) =>
    {
        var result = await reportingService.GetRemainingBudgetAsync(familyId, month, cancellationToken);
        return result is null
            ? Results.NotFound()
            : Results.Ok(MapRemainingBudgetReportResponse(result));
    })
    .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
    .WithName("GetRemainingBudgetReport")
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

static FamilyMemberResponse MapFamilyMemberResponse(FamilyMemberDetails member)
{
    return new FamilyMemberResponse(
        member.Id,
        member.FamilyId,
        member.KeycloakUserId,
        member.Name,
        member.Email,
        member.Role);
}

static AccountResponse MapAccountResponse(AccountDetails account)
{
    return new AccountResponse(
        account.Id,
        account.FamilyId,
        account.Name,
        account.Type,
        account.Balance);
}

static TransactionResponse MapTransactionResponse(TransactionDetails transaction)
{
    return new TransactionResponse(
        transaction.Id,
        transaction.AccountId,
        transaction.Amount,
        transaction.Description,
        transaction.Merchant,
        transaction.OccurredAt,
        transaction.Category,
        transaction.EnvelopeId,
        transaction.Splits.Select(static split => new TransactionSplitResponse(
                split.Id,
                split.TransactionId,
                split.EnvelopeId,
                split.Amount,
                split.Category,
                split.Notes))
            .ToArray());
}

static EnvelopeResponse MapEnvelopeResponse(EnvelopeDetails envelope)
{
    return new EnvelopeResponse(
        envelope.Id,
        envelope.FamilyId,
        envelope.Name,
        envelope.MonthlyBudget,
        envelope.CurrentBalance,
        envelope.LastActivityAt,
        envelope.IsArchived);
}

static BudgetResponse MapBudgetResponse(BudgetDetails budget)
{
    return new BudgetResponse(
        budget.Id,
        budget.FamilyId,
        budget.Month,
        budget.TotalIncome,
        budget.AllocatedAmount,
        budget.RemainingAmount);
}

static EnvelopeBalanceReportResponse MapEnvelopeBalanceReportResponse(EnvelopeBalanceReportDetails details)
{
    return new EnvelopeBalanceReportResponse(
        details.EnvelopeId,
        details.EnvelopeName,
        details.MonthlyBudget,
        details.CurrentBalance,
        details.IsArchived);
}

static MonthlySpendReportPointResponse MapMonthlySpendReportPointResponse(MonthlySpendReportPointDetails details)
{
    return new MonthlySpendReportPointResponse(details.Month, details.TotalSpend);
}

static CategoryBreakdownReportItemResponse MapCategoryBreakdownReportItemResponse(CategoryBreakdownReportItemDetails details)
{
    return new CategoryBreakdownReportItemResponse(details.Category, details.TotalSpend);
}

static RemainingBudgetReportResponse MapRemainingBudgetReportResponse(BudgetDetails budget)
{
    return new RemainingBudgetReportResponse(
        budget.Id,
        budget.FamilyId,
        budget.Month,
        budget.TotalIncome,
        budget.AllocatedAmount,
        budget.RemainingAmount);
}

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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
