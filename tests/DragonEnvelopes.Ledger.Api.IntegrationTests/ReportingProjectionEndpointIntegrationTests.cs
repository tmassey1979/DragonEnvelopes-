using System.Net;
using System.Net.Http.Json;
using DragonEnvelopes.Contracts.Envelopes;
using DragonEnvelopes.Contracts.Reports;
using DragonEnvelopes.Contracts.Transactions;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DragonEnvelopes.Ledger.Api.IntegrationTests;

public sealed class ReportingProjectionEndpointIntegrationTests : IClassFixture<LedgerApiFactory>
{
    private readonly LedgerApiFactory _factory;

    public ReportingProjectionEndpointIntegrationTests(LedgerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Replay_Projects_ReadModels_Used_By_Report_Endpoints()
    {
        var userId = "ledger-projection-user-a";
        var familyId = Guid.Parse("e1400000-0000-0000-0000-000000000001");
        var otherFamilyId = Guid.Parse("e1400000-0000-0000-0000-000000000002");

        using var client = _factory.CreateClient();
        var accountId = await SeedFamilyMembershipAndAccountAsync(userId, familyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var createEnvelopeResponse = await client.PostAsJsonAsync(
            "/api/v1/envelopes",
            new CreateEnvelopeRequest(familyId, "Projection Groceries", 220m));
        Assert.Equal(HttpStatusCode.Created, createEnvelopeResponse.StatusCode);

        var createEnvelopePayload = await createEnvelopeResponse.Content.ReadFromJsonAsync<EnvelopeResponse>();
        Assert.NotNull(createEnvelopePayload);

        var createdFirstTransaction = await client.PostAsJsonAsync(
            "/api/v1/transactions",
            new CreateTransactionRequest(
                accountId,
                -100m,
                "Groceries",
                "Dragon Market",
                new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero),
                "Food",
                EnvelopeId: null,
                Splits: null));
        Assert.Equal(HttpStatusCode.Created, createdFirstTransaction.StatusCode);

        var createdSecondTransaction = await client.PostAsJsonAsync(
            "/api/v1/transactions",
            new CreateTransactionRequest(
                accountId,
                -40m,
                "Fuel",
                "Dragon Fuel",
                new DateTimeOffset(2026, 1, 12, 12, 0, 0, TimeSpan.Zero),
                "Transport",
                EnvelopeId: null,
                Splits: null));
        Assert.Equal(HttpStatusCode.Created, createdSecondTransaction.StatusCode);

        var replayResponse = await client.PostAsync(
            $"/api/v1/reports/projections/replay?familyId={familyId}&batchSize=500",
            content: null);
        var replayPayload = await replayResponse.Content.ReadFromJsonAsync<ReportingProjectionReplayResponse>();

        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.NotNull(replayPayload);
        Assert.True(replayPayload!.AppliedCount >= 3);
        Assert.True(replayPayload.EnvelopeProjectionRowCount >= 1);
        Assert.True(replayPayload.TransactionProjectionRowCount >= 2);

        var statusResponse = await client.GetAsync($"/api/v1/reports/projections/status?familyId={familyId}");
        var statusPayload = await statusResponse.Content.ReadFromJsonAsync<ReportingProjectionStatusResponse>();

        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        Assert.NotNull(statusPayload);
        Assert.Equal(0, statusPayload!.PendingCount);
        Assert.True(statusPayload.AppliedCount >= 3);
        Assert.True(statusPayload.EnvelopeProjectionRowCount >= 1);
        Assert.True(statusPayload.TransactionProjectionRowCount >= 2);

        var balancesResponse = await client.GetAsync($"/api/v1/reports/envelope-balances?familyId={familyId}");
        var balancesPayload = await balancesResponse.Content.ReadFromJsonAsync<List<EnvelopeBalanceReportResponse>>();

        Assert.Equal(HttpStatusCode.OK, balancesResponse.StatusCode);
        Assert.NotNull(balancesPayload);
        Assert.Contains(
            balancesPayload!,
            item => item.EnvelopeId == createEnvelopePayload!.Id && item.EnvelopeName == "Projection Groceries");

        var monthlyResponse = await client.GetAsync(
            $"/api/v1/reports/monthly-spend?familyId={familyId}&from=2026-01-01T00:00:00Z&to=2026-01-31T23:59:59Z");
        var monthlyPayload = await monthlyResponse.Content.ReadFromJsonAsync<List<MonthlySpendReportPointResponse>>();

        Assert.Equal(HttpStatusCode.OK, monthlyResponse.StatusCode);
        Assert.NotNull(monthlyPayload);
        Assert.Single(monthlyPayload!);
        Assert.Equal("2026-01", monthlyPayload[0].Month);
        Assert.Equal(140m, monthlyPayload[0].TotalSpend);

        var categoryResponse = await client.GetAsync(
            $"/api/v1/reports/category-breakdown?familyId={familyId}&from=2026-01-01T00:00:00Z&to=2026-01-31T23:59:59Z");
        var categoryPayload = await categoryResponse.Content.ReadFromJsonAsync<List<CategoryBreakdownReportItemResponse>>();

        Assert.Equal(HttpStatusCode.OK, categoryResponse.StatusCode);
        Assert.NotNull(categoryPayload);
        Assert.Equal(2, categoryPayload!.Count);
        Assert.Equal("Food", categoryPayload[0].Category);
        Assert.Equal(100m, categoryPayload[0].TotalSpend);
        Assert.Equal("Transport", categoryPayload[1].Category);
        Assert.Equal(40m, categoryPayload[1].TotalSpend);
    }

    [Fact]
    public async Task Replay_And_Status_Enforce_Family_Access_When_FamilyId_Provided()
    {
        var userId = "ledger-projection-user-b";
        var ownFamilyId = Guid.Parse("e1400000-0000-0000-0000-000000000011");
        var otherFamilyId = Guid.Parse("e1400000-0000-0000-0000-000000000012");

        using var client = _factory.CreateClient();
        _ = await SeedFamilyMembershipAndAccountAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var statusResponse = await client.GetAsync($"/api/v1/reports/projections/status?familyId={otherFamilyId}");
        var replayResponse = await client.PostAsync(
            $"/api/v1/reports/projections/replay?familyId={otherFamilyId}&batchSize=250",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, statusResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, replayResponse.StatusCode);
    }

    private async Task<Guid> SeedFamilyMembershipAndAccountAsync(
        string userId,
        Guid ownFamilyId,
        Guid otherFamilyId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        dbContext.ReportProjectionAppliedEvents.RemoveRange(dbContext.ReportProjectionAppliedEvents);
        dbContext.ReportTransactionProjections.RemoveRange(dbContext.ReportTransactionProjections);
        dbContext.ReportEnvelopeBalanceProjections.RemoveRange(dbContext.ReportEnvelopeBalanceProjections);
        dbContext.IntegrationOutboxMessages.RemoveRange(dbContext.IntegrationOutboxMessages);
        dbContext.TransactionSplits.RemoveRange(dbContext.TransactionSplits);
        dbContext.Transactions.RemoveRange(dbContext.Transactions);
        dbContext.Envelopes.RemoveRange(dbContext.Envelopes);
        dbContext.Accounts.RemoveRange(dbContext.Accounts);
        dbContext.FamilyMembers.RemoveRange(dbContext.FamilyMembers);
        dbContext.Families.RemoveRange(dbContext.Families);

        var now = DateTimeOffset.UtcNow;
        var ownAccountId = Guid.NewGuid();

        dbContext.Families.AddRange(
            new Family(ownFamilyId, "Projection Authorized Family", now),
            new Family(otherFamilyId, "Projection Forbidden Family", now));

        dbContext.FamilyMembers.Add(new FamilyMember(
            Guid.NewGuid(),
            ownFamilyId,
            userId,
            "Projection Parent User",
            EmailAddress.Parse("projection.parent@test.local"),
            MemberRole.Parent));

        dbContext.Accounts.AddRange(
            new Account(ownAccountId, ownFamilyId, "Projection Checking", AccountType.Checking, Money.FromDecimal(800m)),
            new Account(Guid.NewGuid(), otherFamilyId, "Other Checking", AccountType.Checking, Money.FromDecimal(800m)));

        await dbContext.SaveChangesAsync();
        return ownAccountId;
    }
}
