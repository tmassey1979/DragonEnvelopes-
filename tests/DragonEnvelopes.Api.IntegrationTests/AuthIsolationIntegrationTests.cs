using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Contracts.Financial;
using DragonEnvelopes.Contracts.Onboarding;
using DragonEnvelopes.Contracts.Runtime;
using DragonEnvelopes.Contracts.Transactions;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using DragonEnvelopes.Application.Interfaces;
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
    public async Task UserA_Can_Create_List_And_Cancel_Family_Invite()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var createResponse = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/invites", new
        {
            email = "invitee@test.dev",
            role = "Adult",
            expiresInHours = 72
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(created);
        Assert.False(string.IsNullOrWhiteSpace(created!.InviteToken));
        Assert.Equal("Pending", created.Invite.Status);

        var listResponse = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/invites");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var list = await listResponse.Content.ReadFromJsonAsync<List<FamilyInviteResponse>>();
        Assert.NotNull(list);
        Assert.Contains(list!, invite => invite.Id == created.Invite.Id);

        var cancelResponse = await client.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/invites/{created.Invite.Id}/cancel",
            content: null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<FamilyInviteResponse>();
        Assert.NotNull(cancelled);
        Assert.Equal("Cancelled", cancelled!.Status);
    }

    [Fact]
    public async Task UserA_Cannot_Create_Invite_For_FamilyB()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/invites", new
        {
            email = "blocked@test.dev",
            role = "Adult",
            expiresInHours = 24
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Invite_Can_Be_Accepted_Anonymously_By_Token()
    {
        using var authorizedClient = _factory.CreateClient();
        authorizedClient.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var createResponse = await authorizedClient.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/invites", new
        {
            email = "accept@test.dev",
            role = "Teen",
            expiresInHours = 24
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(created);

        using var anonymousClient = _factory.CreateClient();
        var acceptResponse = await anonymousClient.PostAsJsonAsync("/api/v1/families/invites/accept", new
        {
            inviteToken = created!.InviteToken
        });

        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        var accepted = await acceptResponse.Content.ReadFromJsonAsync<FamilyInviteResponse>();
        Assert.NotNull(accepted);
        Assert.Equal("Accepted", accepted!.Status);
    }

    [Fact]
    public async Task RecurringExecutionRepository_HasExecution_ChecksIdempotencyKey()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRecurringBillExecutionRepository>();

        var dueDate = new DateOnly(2026, 3, 1);
        var initialHasExecution = await repo.HasExecutionAsync(TestApiFactory.RecurringBillAId, dueDate);
        Assert.False(initialHasExecution);

        await repo.AddAsync(new RecurringBillExecution(
            Guid.NewGuid(),
            TestApiFactory.RecurringBillAId,
            TestApiFactory.FamilyAId,
            dueDate,
            DateTimeOffset.UtcNow,
            transactionId: null,
            result: "Posted",
            notes: "Integration test"));

        var finalHasExecution = await repo.HasExecutionAsync(TestApiFactory.RecurringBillAId, dueDate);
        Assert.True(finalHasExecution);
    }

    [Fact]
    public async Task UserA_Can_Get_And_Update_Own_Onboarding_Profile()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var getResponse = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/onboarding");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var initial = await getResponse.Content.ReadFromJsonAsync<OnboardingProfileResponse>();
        Assert.NotNull(initial);
        Assert.False(initial!.AccountsCompleted);
        Assert.False(initial.EnvelopesCompleted);
        Assert.False(initial.BudgetCompleted);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/onboarding", new
        {
            accountsCompleted = true,
            envelopesCompleted = true,
            budgetCompleted = true
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<OnboardingProfileResponse>();
        Assert.NotNull(updated);
        Assert.True(updated!.IsCompleted);
        Assert.NotNull(updated.CompletedAtUtc);
    }

    [Fact]
    public async Task UserA_Cannot_Update_FamilyB_Onboarding_Profile()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/onboarding", new
        {
            accountsCompleted = true,
            envelopesCompleted = true,
            budgetCompleted = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Run_Onboarding_Bootstrap_For_Own_Family()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/onboarding/bootstrap", new
        {
            accounts = new[]
            {
                new
                {
                    name = "Starter Savings",
                    type = "Savings",
                    openingBalance = 250.00m
                }
            },
            envelopes = new[]
            {
                new
                {
                    name = "Utilities",
                    monthlyBudget = 180.00m
                }
            },
            budget = new
            {
                month = "2027-01",
                totalIncome = 5000.00m
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<OnboardingBootstrapResponse>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.AccountsCreated);
        Assert.Equal(1, payload.EnvelopesCreated);
        Assert.True(payload.BudgetCreated);
    }

    [Fact]
    public async Task UserA_Cannot_Run_Onboarding_Bootstrap_For_FamilyB()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/onboarding/bootstrap", new
        {
            accounts = new object[] { },
            envelopes = new object[] { },
            budget = new
            {
                month = "2027-02",
                totalIncome = 5000.00m
            }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Get_Own_Financial_Status()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/financial/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<FamilyFinancialStatusResponse>();
        Assert.NotNull(payload);
        Assert.Equal(TestApiFactory.FamilyAId, payload!.FamilyId);
        Assert.False(payload.PlaidConnected);
        Assert.False(payload.StripeConnected);
    }

    [Fact]
    public async Task UserA_Cannot_Get_FamilyB_Financial_Status()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/financial/status");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Onboarding_Bootstrap_Rejects_Duplicate_Account_Names_In_Request()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/onboarding/bootstrap", new
        {
            accounts = new[]
            {
                new { name = "Duplicate Name", type = "Checking", openingBalance = 50m },
                new { name = "Duplicate Name", type = "Savings", openingBalance = 10m }
            },
            envelopes = Array.Empty<object>(),
            budget = (object?)null
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
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
    public static readonly Guid RecurringBillAId = Guid.Parse("12345678-90ab-cdef-1234-567890abcdef");
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

        dbContext.RecurringBills.Add(
            new RecurringBill(
                RecurringBillAId,
                FamilyAId,
                "Rent",
                "Landlord",
                Money.FromDecimal(1000m),
                RecurringBillFrequency.Monthly,
                dayOfMonth: 1,
                startDate: new DateOnly(2026, 1, 1),
                endDate: null,
                isActive: true));

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
