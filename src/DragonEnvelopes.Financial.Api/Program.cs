using DragonEnvelopes.Financial.Api.Bootstrap;

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
var validIssuers = FinancialApiBootstrap.BuildValidIssuers(authority, publicAuthority);

FinancialApiBootstrap.ConfigureSerilog(builder);
FinancialApiBootstrap.ConfigureOpenApi(builder.Services);
FinancialApiBootstrap.ConfigureApiVersioning(builder.Services);
FinancialApiBootstrap.ConfigureAuthenticationAndAuthorization(
    builder,
    authority,
    audience,
    validIssuers,
    allowedAuthorizedParties);
FinancialApiBootstrap.ConfigureDependencyInjection(builder);
FinancialApiBootstrap.ConfigureHealthChecks(builder.Services, builder.Configuration.GetConnectionString("Default"));

var app = builder.Build();

FinancialApiBootstrap.ApplyDatabaseMigrations(app);
FinancialApiBootstrap.ConfigureMiddleware(app);
FinancialApiBootstrap.MapHealthEndpoints(app);
FinancialApiBootstrap.MapVersionedEndpoints(app);

app.Run();

public partial class Program
{
}

