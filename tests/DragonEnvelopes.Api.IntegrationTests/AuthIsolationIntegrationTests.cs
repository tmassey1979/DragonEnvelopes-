using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DragonEnvelopes.Contracts.Runtime;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Api.IntegrationTests;

public sealed class AuthIsolationIntegrationTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public AuthIsolationIntegrationTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unauthorized_Request_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/accounts?familyId={TestApiFactory.FamilyAId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Read_FamilyB_Accounts()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/accounts?familyId={TestApiFactory.FamilyBId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Create_Transaction_For_FamilyB_Account()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            accountId = TestApiFactory.AccountBId,
            amount = -10.00m,
            description = "Leak Attempt",
            merchant = "Other Store",
            occurredAt = DateTimeOffset.UtcNow,
            category = "Misc",
            envelopeId = (Guid?)null,
            splits = (object?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reports_Are_Isolated_By_Family()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var allowed = await client.GetAsync($"/api/v1/reports/envelope-balances?familyId={TestApiFactory.FamilyAId}");
        var forbidden = await client.GetAsync($"/api/v1/reports/envelope-balances?familyId={TestApiFactory.FamilyBId}");

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task System_Health_Is_Available_Anonymously()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/system/health");
        var payload = await response.Content.ReadFromJsonAsync<ApiHealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Healthy", payload!.Status);
    }

    [Fact]
    public async Task System_Version_Is_Available_Anonymously()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/system/version");
        var payload = await response.Content.ReadFromJsonAsync<ApiVersionResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Version));
        Assert.False(string.IsNullOrWhiteSpace(payload.Environment));
    }
}

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    public static readonly Guid FamilyAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid FamilyBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid AccountAId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    public static readonly Guid AccountBId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
    public const string UserAId = "user-a";
    public const string UserBId = "user-b";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = "http://test-authority",
                ["Authentication:Audience"] = "dragonenvelopes-api"
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(
                descriptor => descriptor.ServiceType == typeof(DbContextOptions<DragonEnvelopesDbContext>));
            if (dbDescriptor is not null)
            {
                services.Remove(dbDescriptor);
            }

            services.AddDbContext<DragonEnvelopesDbContext>(options =>
                options.UseInMemoryDatabase("dragonenvelopes-auth-isolation-tests"));

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            Seed(dbContext);
        });
    }

    private static void Seed(DragonEnvelopesDbContext dbContext)
    {
        var familyA = new Family(FamilyAId, "Family A", DateTimeOffset.UtcNow);
        var familyB = new Family(FamilyBId, "Family B", DateTimeOffset.UtcNow);
        dbContext.Families.AddRange(familyA, familyB);

        dbContext.FamilyMembers.AddRange(
            new FamilyMember(Guid.NewGuid(), FamilyAId, UserAId, "User A", EmailAddress.Parse("a@test.dev"), MemberRole.Parent),
            new FamilyMember(Guid.NewGuid(), FamilyBId, UserBId, "User B", EmailAddress.Parse("b@test.dev"), MemberRole.Parent));

        dbContext.Accounts.AddRange(
            new Account(AccountAId, FamilyAId, "Checking A", AccountType.Checking, Money.FromDecimal(1000m)),
            new Account(AccountBId, FamilyBId, "Checking B", AccountType.Checking, Money.FromDecimal(1000m)));

        dbContext.Envelopes.AddRange(
            new Envelope(Guid.NewGuid(), FamilyAId, "Groceries A", Money.FromDecimal(200m), Money.FromDecimal(100m)),
            new Envelope(Guid.NewGuid(), FamilyBId, "Groceries B", Money.FromDecimal(200m), Money.FromDecimal(100m)));

        dbContext.SaveChanges();
    }
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestAuth";
    public const string UserHeader = "X-Test-User";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeader, out var userId) || string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.Role, "Parent"),
            new Claim("preferred_username", userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, SchemeName, "preferred_username", ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
