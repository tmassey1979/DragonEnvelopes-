using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DragonEnvelopes.Contracts.Accounts;
using DragonEnvelopes.Contracts.Budgets;
using DragonEnvelopes.Contracts.Envelopes;
using DragonEnvelopes.Contracts.RecurringBills;
using DragonEnvelopes.Contracts.Reports;
using DragonEnvelopes.Contracts.Transactions;
using DragonEnvelopes.Ledger.Api.CrossCutting.Auth;
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

namespace DragonEnvelopes.Ledger.Api.IntegrationTests;

public sealed class LedgerApiSmokeTests : IClassFixture<LedgerApiFactory>
{
    private readonly LedgerApiFactory _factory;

    public LedgerApiSmokeTests(LedgerApiFactory factory)
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

        var response = await client.GetAsync($"/api/v1/accounts?familyId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_Can_List_Own_Family_Accounts_But_Not_Other_Family()
    {
        var userId = "ledger-user-a";
        var ownFamilyId = Guid.Parse("e1000000-0000-0000-0000-000000000001");
        var otherFamilyId = Guid.Parse("e1000000-0000-0000-0000-000000000002");

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAndAccountAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var ownResponse = await client.GetAsync($"/api/v1/accounts?familyId={ownFamilyId}");
        var ownPayload = await ownResponse.Content.ReadFromJsonAsync<List<AccountResponse>>();

        var otherResponse = await client.GetAsync($"/api/v1/accounts?familyId={otherFamilyId}");

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.NotNull(ownPayload);
        Assert.Single(ownPayload!);
        Assert.Equal("Primary Checking", ownPayload[0].Name);
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_Can_List_Own_Family_Envelopes_But_Not_Other_Family()
    {
        var userId = "ledger-user-a";
        var ownFamilyId = Guid.Parse("e1000000-0000-0000-0000-000000000011");
        var otherFamilyId = Guid.Parse("e1000000-0000-0000-0000-000000000012");

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAndAccountAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var ownResponse = await client.GetAsync($"/api/v1/envelopes?familyId={ownFamilyId}");
        var ownPayload = await ownResponse.Content.ReadFromJsonAsync<List<EnvelopeResponse>>();

        var otherResponse = await client.GetAsync($"/api/v1/envelopes?familyId={otherFamilyId}");

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.NotNull(ownPayload);
        Assert.Single(ownPayload!);
        Assert.Equal("Groceries", ownPayload[0].Name);
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_Can_Get_Own_Budget_By_Month_But_Not_Other_Family()
    {
        var userId = "ledger-user-a";
        var ownFamilyId = Guid.Parse("e1000000-0000-0000-0000-000000000021");
        var otherFamilyId = Guid.Parse("e1000000-0000-0000-0000-000000000022");
        const string month = "2026-03";

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAndBudgetsAsync(userId, ownFamilyId, otherFamilyId, month);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var ownResponse = await client.GetAsync($"/api/v1/budgets/{ownFamilyId}/{month}");
        var ownPayload = await ownResponse.Content.ReadFromJsonAsync<BudgetResponse>();

        var otherResponse = await client.GetAsync($"/api/v1/budgets/{otherFamilyId}/{month}");

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.NotNull(ownPayload);
        Assert.Equal(ownFamilyId, ownPayload!.FamilyId);
        Assert.Equal(month, ownPayload.Month);
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_Can_Get_Own_EnvelopeBalances_Report_But_Not_Other_Family()
    {
        var userId = "ledger-user-a";
        var ownFamilyId = Guid.Parse("e1000000-0000-0000-0000-000000000031");
        var otherFamilyId = Guid.Parse("e1000000-0000-0000-0000-000000000032");

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAndAccountAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var ownResponse = await client.GetAsync($"/api/v1/reports/envelope-balances?familyId={ownFamilyId}");
        var ownPayload = await ownResponse.Content.ReadFromJsonAsync<List<EnvelopeBalanceReportResponse>>();

        var otherResponse = await client.GetAsync($"/api/v1/reports/envelope-balances?familyId={otherFamilyId}");

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.NotNull(ownPayload);
        Assert.NotEmpty(ownPayload!);
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_Can_SoftDelete_Own_Family_Transaction()
    {
        var userId = "ledger-user-a";
        var ownFamilyId = Guid.Parse("e2000000-0000-0000-0000-000000000001");
        var otherFamilyId = Guid.Parse("e2000000-0000-0000-0000-000000000002");

        using var client = _factory.CreateClient();
        var seeded = await SeedFamilyMembershipAndTransactionsAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var response = await client.DeleteAsync($"/api/v1/transactions/{seeded.OwnTransactionId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
        var transaction = await dbContext.Transactions.FirstOrDefaultAsync(x => x.Id == seeded.OwnTransactionId);
        Assert.NotNull(transaction);
        Assert.NotNull(transaction!.DeletedAtUtc);
        Assert.Equal(userId, transaction.DeletedByUserId);
    }

    [Fact]
    public async Task Authenticated_User_Cannot_Delete_Other_Family_Transaction()
    {
        var userId = "ledger-user-a";
        var ownFamilyId = Guid.Parse("e3000000-0000-0000-0000-000000000001");
        var otherFamilyId = Guid.Parse("e3000000-0000-0000-0000-000000000002");

        using var client = _factory.CreateClient();
        var seeded = await SeedFamilyMembershipAndTransactionsAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var response = await client.DeleteAsync($"/api/v1/transactions/{seeded.OtherTransactionId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_Can_Restore_Own_SoftDeleted_Transaction()
    {
        var userId = "ledger-user-restore-a";
        var ownFamilyId = Guid.Parse("e3000000-0000-0000-0000-000000000011");
        var otherFamilyId = Guid.Parse("e3000000-0000-0000-0000-000000000012");

        using var client = _factory.CreateClient();
        var seeded = await SeedFamilyMembershipAndTransactionsAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var deleteResponse = await client.DeleteAsync($"/api/v1/transactions/{seeded.OwnTransactionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var restoreResponse = await client.PostAsync($"/api/v1/transactions/{seeded.OwnTransactionId}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
        var restoredPayload = await restoreResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(restoredPayload);
        Assert.Null(restoredPayload!.DeletedAtUtc);
        Assert.Null(restoredPayload.DeletedByUserId);
    }

    [Fact]
    public async Task Authenticated_User_Can_List_Deleted_Transactions_For_Own_Family()
    {
        var userId = "ledger-user-deleted-list-a";
        var ownFamilyId = Guid.Parse("e3000000-0000-0000-0000-000000000021");
        var otherFamilyId = Guid.Parse("e3000000-0000-0000-0000-000000000022");

        using var client = _factory.CreateClient();
        var seeded = await SeedFamilyMembershipAndTransactionsAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var deleteResponse = await client.DeleteAsync($"/api/v1/transactions/{seeded.OwnTransactionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var ownResponse = await client.GetAsync($"/api/v1/transactions/deleted?familyId={ownFamilyId}&days=30");
        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        var ownPayload = await ownResponse.Content.ReadFromJsonAsync<List<TransactionResponse>>();
        Assert.NotNull(ownPayload);
        Assert.Contains(ownPayload!, transaction => transaction.Id == seeded.OwnTransactionId && transaction.DeletedAtUtc.HasValue);

        var otherResponse = await client.GetAsync($"/api/v1/transactions/deleted?familyId={otherFamilyId}&days=30");
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_Can_List_Own_RecurringBills_But_Not_Other_Family()
    {
        var userId = "ledger-user-recurring-a";
        var ownFamilyId = Guid.Parse("e4000000-0000-0000-0000-000000000001");
        var otherFamilyId = Guid.Parse("e4000000-0000-0000-0000-000000000002");

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAndRecurringBillsAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var ownResponse = await client.GetAsync($"/api/v1/recurring-bills?familyId={ownFamilyId}");
        var ownPayload = await ownResponse.Content.ReadFromJsonAsync<List<RecurringBillResponse>>();

        var otherResponse = await client.GetAsync($"/api/v1/recurring-bills?familyId={otherFamilyId}");

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.NotNull(ownPayload);
        Assert.Single(ownPayload!);
        Assert.Equal("Recurring Rent", ownPayload[0].Name);
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    private async Task SeedFamilyMembershipAndAccountAsync(string userId, Guid ownFamilyId, Guid otherFamilyId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        dbContext.TransactionSplits.RemoveRange(dbContext.TransactionSplits);
        dbContext.Transactions.RemoveRange(dbContext.Transactions);
        dbContext.Envelopes.RemoveRange(dbContext.Envelopes);
        dbContext.Accounts.RemoveRange(dbContext.Accounts);
        dbContext.FamilyMembers.RemoveRange(dbContext.FamilyMembers);
        dbContext.Families.RemoveRange(dbContext.Families);

        var now = DateTimeOffset.UtcNow;
        dbContext.Families.AddRange(
            new Family(ownFamilyId, "Authorized Ledger Family", now),
            new Family(otherFamilyId, "Forbidden Ledger Family", now));

        dbContext.FamilyMembers.Add(new FamilyMember(
            Guid.NewGuid(),
            ownFamilyId,
            userId,
            "Ledger Parent User",
            EmailAddress.Parse("ledger.parent@test.local"),
            MemberRole.Parent));

        dbContext.Accounts.Add(new Account(
            Guid.NewGuid(),
            ownFamilyId,
            "Primary Checking",
            AccountType.Checking,
            Money.FromDecimal(500m)));

        dbContext.Envelopes.AddRange(
            new Envelope(
                Guid.NewGuid(),
                ownFamilyId,
                "Groceries",
                Money.FromDecimal(200m),
                Money.FromDecimal(150m)),
            new Envelope(
                Guid.NewGuid(),
                otherFamilyId,
                "Other Family Envelope",
                Money.FromDecimal(300m),
                Money.FromDecimal(250m)));

        await dbContext.SaveChangesAsync();
    }

    private async Task<(Guid OwnTransactionId, Guid OtherTransactionId)> SeedFamilyMembershipAndTransactionsAsync(
        string userId,
        Guid ownFamilyId,
        Guid otherFamilyId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        dbContext.TransactionSplits.RemoveRange(dbContext.TransactionSplits);
        dbContext.Transactions.RemoveRange(dbContext.Transactions);
        dbContext.Accounts.RemoveRange(dbContext.Accounts);
        dbContext.FamilyMembers.RemoveRange(dbContext.FamilyMembers);
        dbContext.Families.RemoveRange(dbContext.Families);

        var now = DateTimeOffset.UtcNow;
        var ownAccountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        var ownTransactionId = Guid.NewGuid();
        var otherTransactionId = Guid.NewGuid();

        dbContext.Families.AddRange(
            new Family(ownFamilyId, "Authorized Ledger Family", now),
            new Family(otherFamilyId, "Forbidden Ledger Family", now));

        dbContext.FamilyMembers.Add(new FamilyMember(
            Guid.NewGuid(),
            ownFamilyId,
            userId,
            "Ledger Parent User",
            EmailAddress.Parse("ledger.parent@test.local"),
            MemberRole.Parent));

        dbContext.Accounts.AddRange(
            new Account(ownAccountId, ownFamilyId, "Primary Checking", AccountType.Checking, Money.FromDecimal(500m)),
            new Account(otherAccountId, otherFamilyId, "Other Checking", AccountType.Checking, Money.FromDecimal(500m)));

        dbContext.Transactions.AddRange(
            new Transaction(
                ownTransactionId,
                ownAccountId,
                Money.FromDecimal(-15m),
                "Own Txn",
                "Store A",
                now,
                "Misc"),
            new Transaction(
                otherTransactionId,
                otherAccountId,
                Money.FromDecimal(-22m),
                "Other Txn",
                "Store B",
                now,
                "Misc"));

        await dbContext.SaveChangesAsync();
        return (ownTransactionId, otherTransactionId);
    }

    private async Task SeedFamilyMembershipAndBudgetsAsync(
        string userId,
        Guid ownFamilyId,
        Guid otherFamilyId,
        string month)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        dbContext.Budgets.RemoveRange(dbContext.Budgets);
        dbContext.FamilyMembers.RemoveRange(dbContext.FamilyMembers);
        dbContext.Families.RemoveRange(dbContext.Families);

        var now = DateTimeOffset.UtcNow;
        dbContext.Families.AddRange(
            new Family(ownFamilyId, "Authorized Ledger Family", now),
            new Family(otherFamilyId, "Forbidden Ledger Family", now));

        dbContext.FamilyMembers.Add(new FamilyMember(
            Guid.NewGuid(),
            ownFamilyId,
            userId,
            "Ledger Parent User",
            EmailAddress.Parse("ledger.parent@test.local"),
            MemberRole.Parent));

        dbContext.Budgets.AddRange(
            new Budget(Guid.NewGuid(), ownFamilyId, BudgetMonth.Parse(month), Money.FromDecimal(5000m)),
            new Budget(Guid.NewGuid(), otherFamilyId, BudgetMonth.Parse(month), Money.FromDecimal(7000m)));

        await dbContext.SaveChangesAsync();
    }

    private async Task SeedFamilyMembershipAndRecurringBillsAsync(
        string userId,
        Guid ownFamilyId,
        Guid otherFamilyId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        dbContext.RecurringBillExecutions.RemoveRange(dbContext.RecurringBillExecutions);
        dbContext.RecurringBills.RemoveRange(dbContext.RecurringBills);
        dbContext.FamilyMembers.RemoveRange(dbContext.FamilyMembers);
        dbContext.Families.RemoveRange(dbContext.Families);

        var now = DateTimeOffset.UtcNow;
        dbContext.Families.AddRange(
            new Family(ownFamilyId, "Authorized Ledger Family", now),
            new Family(otherFamilyId, "Forbidden Ledger Family", now));

        dbContext.FamilyMembers.Add(new FamilyMember(
            Guid.NewGuid(),
            ownFamilyId,
            userId,
            "Ledger Parent User",
            EmailAddress.Parse("ledger.parent@test.local"),
            MemberRole.Parent));

        dbContext.RecurringBills.AddRange(
            new RecurringBill(
                Guid.NewGuid(),
                ownFamilyId,
                "Recurring Rent",
                "Landlord",
                Money.FromDecimal(1500m),
                RecurringBillFrequency.Monthly,
                dayOfMonth: 1,
                startDate: DateOnly.FromDateTime(DateTime.UtcNow.Date),
                endDate: null,
                isActive: true),
            new RecurringBill(
                Guid.NewGuid(),
                otherFamilyId,
                "Forbidden Utility",
                "Utilities",
                Money.FromDecimal(200m),
                RecurringBillFrequency.Monthly,
                dayOfMonth: 5,
                startDate: DateOnly.FromDateTime(DateTime.UtcNow.Date),
                endDate: null,
                isActive: true));

        await dbContext.SaveChangesAsync();
    }
}

public sealed class LedgerApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ledger-api-smoke-{Guid.NewGuid()}";

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
