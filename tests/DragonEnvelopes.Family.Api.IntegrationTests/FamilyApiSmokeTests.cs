using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DragonEnvelopes.Family.Api.CrossCutting.Auth;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Family.Api.IntegrationTests;

public sealed class FamilyApiSmokeTests : IClassFixture<FamilyApiFactory>
{
    private readonly FamilyApiFactory _factory;

    public FamilyApiSmokeTests(FamilyApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Live_Returns_Ok()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Protected_Endpoint_Without_Auth_Returns_Unauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/families/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_Can_Access_Own_Family_But_Not_Other_Family()
    {
        var userId = "family-user-a";
        var ownFamilyId = Guid.Parse("d1000000-0000-0000-0000-000000000001");
        var otherFamilyId = Guid.Parse("d1000000-0000-0000-0000-000000000002");

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var ownResponse = await client.GetAsync($"/api/v1/families/{ownFamilyId}");
        var otherResponse = await client.GetAsync($"/api/v1/families/{otherFamilyId}");

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    private async Task SeedFamilyMembershipAsync(string userId, Guid ownFamilyId, Guid otherFamilyId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        dbContext.Families.RemoveRange(dbContext.Families);
        dbContext.FamilyMembers.RemoveRange(dbContext.FamilyMembers);

        var now = DateTimeOffset.UtcNow;
        dbContext.Families.AddRange(
            new DragonEnvelopes.Domain.Entities.Family(ownFamilyId, "Authorized Family", now),
            new DragonEnvelopes.Domain.Entities.Family(otherFamilyId, "Forbidden Family", now));

        dbContext.FamilyMembers.Add(new FamilyMember(
            Guid.NewGuid(),
            ownFamilyId,
            userId,
            "Parent User",
            EmailAddress.Parse("parent.user@test.local"),
            MemberRole.Parent));

        await dbContext.SaveChangesAsync();
    }
}

public sealed class FamilyApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"family-api-smoke-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = "https://localhost/realms/dragonenvelopes",
                ["Authentication:PublicAuthority"] = "https://localhost/realms/dragonenvelopes",
                ["Authentication:Audience"] = "dragonenvelopes-api",
                ["ConnectionStrings:Default"] = "Host=localhost;Port=5432;Database=dragonenvelopes;Username=postgres;Password=postgres"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<DragonEnvelopesDbContext>>();
            services.RemoveAll<DragonEnvelopesDbContext>();
            services.AddDbContext<DragonEnvelopesDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(ApiAuthorizationPolicies.AnyFamilyMember, policy => policy.RequireAuthenticatedUser());
                options.AddPolicy(ApiAuthorizationPolicies.Parent, policy => policy.RequireAuthenticatedUser());
            });
        });
    }
}

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestAuth";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var userId) || string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim(ClaimTypes.Role, "Parent"),
                new Claim("role", "Parent")
            ],
            SchemeName,
            ClaimTypes.Name,
            ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
