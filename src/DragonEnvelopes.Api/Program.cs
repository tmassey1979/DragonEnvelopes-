using DragonEnvelopes.Api.Bootstrap;

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
var validIssuers = ApiBootstrap.BuildValidIssuers(authority, publicAuthority);

ApiBootstrap.ConfigureSerilog(builder);
ApiBootstrap.ConfigureOpenApi(builder.Services);
ApiBootstrap.ConfigureApiVersioning(builder.Services);
ApiBootstrap.ConfigureAuthenticationAndAuthorization(
    builder,
    authority,
    audience,
    validIssuers,
    allowedAuthorizedParties);
ApiBootstrap.ConfigureDependencyInjection(builder);
ApiBootstrap.ConfigureHealthChecks(
    builder.Services,
    builder.Configuration.GetConnectionString("Default"));

var app = builder.Build();

ApiBootstrap.ApplyDatabaseMigrations(app);
ApiBootstrap.ConfigureMiddleware(app);
ApiBootstrap.MapHealthEndpoints(app);

ApiBootstrap.MapVersionedEndpoints(app);

app.Run();

public partial class Program
{
}
