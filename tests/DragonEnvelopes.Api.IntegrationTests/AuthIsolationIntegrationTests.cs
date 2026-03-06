using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DragonEnvelopes.Contracts.Runtime;
using DragonEnvelopes.Contracts.Transactions;
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
    public async Task UserA_Cannot_Update_FamilyB_Transaction()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PutAsJsonAsync($"/api/v1/transactions/{TestApiFactory.TransactionBId}", new
        {
            description = "Updated",
            merchant = "Updated Merchant",
            category = "Updated Category"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Update_Own_Transaction_Metadata()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PutAsJsonAsync($"/api/v1/transactions/{TestApiFactory.TransactionAId}", new
        {
            description = "Updated Groceries",
            merchant = "Updated Market",
            category = "Food"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Updated Groceries", payload!.Description);
        Assert.Equal("Updated Market", payload.Merchant);
        Assert.Equal("Food", payload.Category);
    }

    [Fact]
    public async Task UserA_Can_Replace_Single_Envelope_With_Splits_And_Rebalance()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PutAsJsonAsync($"/api/v1/transactions/{TestApiFactory.TransactionAId}", new
        {
            description = "Groceries Split",
            merchant = "Market A",
            category = "Food",
            replaceAllocation = true,
            envelopeId = (Guid?)null,
            splits = new object[]
            {
                new
                {
                    envelopeId = TestApiFactory.EnvelopeAId,
                    amount = -4.00m,
                    category = "Food",
                    notes = "Part A"
                },
                new
                {
                    envelopeId = TestApiFactory.EnvelopeA2Id,
                    amount = -6.00m,
                    category = "Food",
                    notes = "Part B"
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        var envelopeA = await dbContext.Envelopes.FirstAsync(x => x.Id == TestApiFactory.EnvelopeAId);
        var envelopeA2 = await dbContext.Envelopes.FirstAsync(x => x.Id == TestApiFactory.EnvelopeA2Id);
        var transaction = await dbContext.Transactions.FirstAsync(x => x.Id == TestApiFactory.TransactionAId);
        var splitCount = await dbContext.TransactionSplits.CountAsync(x => x.TransactionId == TestApiFactory.TransactionAId);

        Assert.Equal(106.00m, envelopeA.CurrentBalance.Amount);
        Assert.Equal(94.00m, envelopeA2.CurrentBalance.Amount);
        Assert.Null(transaction.EnvelopeId);
        Assert.Equal(2, splitCount);
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
    public static readonly Guid EnvelopeAId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    public static readonly Guid EnvelopeA2Id = Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222");
    public static readonly Guid EnvelopeBId = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111");
    public static readonly Guid TransactionAId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    public static readonly Guid TransactionBId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
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
            new Envelope(EnvelopeAId, FamilyAId, "Groceries A", Money.FromDecimal(200m), Money.FromDecimal(100m)),
            new Envelope(EnvelopeA2Id, FamilyAId, "Groceries A2", Money.FromDecimal(200m), Money.FromDecimal(100m)),
            new Envelope(EnvelopeBId, FamilyBId, "Groceries B", Money.FromDecimal(200m), Money.FromDecimal(100m)));

        dbContext.Transactions.AddRange(
            new Transaction(
                TransactionAId,
                AccountAId,
                Money.FromDecimal(-10.00m),
                "Groceries",
                "Market A",
                DateTimeOffset.UtcNow,
                "Food",
                EnvelopeAId),
            new Transaction(
                TransactionBId,
                AccountBId,
                Money.FromDecimal(-11.00m),
                "Coffee",
                "Cafe B",
                DateTimeOffset.UtcNow,
                "Dining"));

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
