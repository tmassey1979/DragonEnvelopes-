using DragonEnvelopes.Ledger.Api.Bootstrap;

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
var validIssuers = LedgerApiBootstrap.BuildValidIssuers(authority, publicAuthority);

LedgerApiBootstrap.ConfigureSerilog(builder);
LedgerApiBootstrap.ConfigureOpenApi(builder.Services);
LedgerApiBootstrap.ConfigureApiVersioning(builder.Services);
LedgerApiBootstrap.ConfigureAuthenticationAndAuthorization(
    builder,
    authority,
    audience,
    validIssuers,
    allowedAuthorizedParties);
LedgerApiBootstrap.ConfigureDependencyInjection(builder);
LedgerApiBootstrap.ConfigureHealthChecks(builder.Services, builder.Configuration.GetConnectionString("Default"));

var app = builder.Build();

LedgerApiBootstrap.ApplyDatabaseMigrations(app);
LedgerApiBootstrap.ConfigureMiddleware(app);
LedgerApiBootstrap.MapHealthEndpoints(app);
LedgerApiBootstrap.MapVersionedEndpoints(app);

app.Run();

public partial class Program
{
}
