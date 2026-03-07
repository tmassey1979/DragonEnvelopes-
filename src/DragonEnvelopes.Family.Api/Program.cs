using DragonEnvelopes.Family.Api.Bootstrap;

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
var validIssuers = FamilyApiBootstrap.BuildValidIssuers(authority, publicAuthority);

FamilyApiBootstrap.ConfigureSerilog(builder);
FamilyApiBootstrap.ConfigureOpenApi(builder.Services);
FamilyApiBootstrap.ConfigureApiVersioning(builder.Services);
FamilyApiBootstrap.ConfigureAuthenticationAndAuthorization(
    builder,
    authority,
    audience,
    validIssuers,
    allowedAuthorizedParties);
FamilyApiBootstrap.ConfigureDependencyInjection(builder);
FamilyApiBootstrap.ConfigureHealthChecks(builder.Services, builder.Configuration.GetConnectionString("Default"));

var app = builder.Build();

FamilyApiBootstrap.ApplyDatabaseMigrations(app);
FamilyApiBootstrap.ConfigureMiddleware(app);
FamilyApiBootstrap.MapHealthEndpoints(app);
FamilyApiBootstrap.MapVersionedEndpoints(app);

app.Run();

public partial class Program
{
}
