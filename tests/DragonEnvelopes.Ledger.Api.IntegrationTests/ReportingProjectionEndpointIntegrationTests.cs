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

    [Fact]
    public async Task Replay_DryRun_Reports_Targets_Without_Changing_Projection_State_And_Audits_Run()
    {
        var userId = "ledger-projection-user-c";
        var familyId = Guid.Parse("e1400000-0000-0000-0000-000000000021");
        var otherFamilyId = Guid.Parse("e1400000-0000-0000-0000-000000000022");

        using var client = _factory.CreateClient();
        var accountId = await SeedFamilyMembershipAndAccountAsync(userId, familyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var createdTransaction = await client.PostAsJsonAsync(
            "/api/v1/transactions",
            new CreateTransactionRequest(
                accountId,
                -75m,
                "Dry Run Groceries",
                "Dragon Market",
                new DateTimeOffset(2026, 1, 13, 12, 0, 0, TimeSpan.Zero),
                "Food",
                EnvelopeId: null,
                Splits: null));
        Assert.Equal(HttpStatusCode.Created, createdTransaction.StatusCode);

        var replayResponse = await client.PostAsync(
            $"/api/v1/reports/projections/replay?familyId={familyId}&projectionSet=Transactions&dryRun=true&batchSize=5",
            content: null);
        var replayPayload = await replayResponse.Content.ReadFromJsonAsync<ReportingProjectionReplayResponse>();

        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.NotNull(replayPayload);
        Assert.True(replayPayload!.IsDryRun);
        Assert.Equal("Transactions", replayPayload.ProjectionSet);
        Assert.True(replayPayload.TargetedEventCount >= 1);
        Assert.Equal(0, replayPayload.ProcessedEventCount);
        Assert.Equal(0, replayPayload.AppliedCount);

        var statusResponse = await client.GetAsync($"/api/v1/reports/projections/status?familyId={familyId}");
        var statusPayload = await statusResponse.Content.ReadFromJsonAsync<ReportingProjectionStatusResponse>();

        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        Assert.NotNull(statusPayload);
        Assert.True(statusPayload!.PendingCount >= 1);
        Assert.Equal(0, statusPayload.AppliedCount);

        var runsResponse = await client.GetAsync($"/api/v1/reports/projections/replay-runs?familyId={familyId}&take=10");
        var runsPayload = await runsResponse.Content.ReadFromJsonAsync<List<ReportingProjectionReplayRunResponse>>();

        Assert.Equal(HttpStatusCode.OK, runsResponse.StatusCode);
        Assert.NotNull(runsPayload);
        var matchingRun = runsPayload!.Single(x => x.Id == replayPayload.ReplayRunId);
        Assert.True(matchingRun.IsDryRun);
        Assert.Equal("Completed", matchingRun.Status);

        var runResponse = await client.GetAsync($"/api/v1/reports/projections/replay-runs/{replayPayload.ReplayRunId}");
        var runPayload = await runResponse.Content.ReadFromJsonAsync<ReportingProjectionReplayRunResponse>();

        Assert.Equal(HttpStatusCode.OK, runResponse.StatusCode);
        Assert.NotNull(runPayload);
        Assert.Equal(replayPayload.ReplayRunId, runPayload!.Id);
        Assert.Equal("Transactions", runPayload.ProjectionSet);
    }

    [Fact]
    public async Task Replay_Can_Target_Projection_Set_And_Event_Range()
    {
        var userId = "ledger-projection-user-d";
        var familyId = Guid.Parse("e1400000-0000-0000-0000-000000000031");
        var otherFamilyId = Guid.Parse("e1400000-0000-0000-0000-000000000032");

        using var client = _factory.CreateClient();
        var accountId = await SeedFamilyMembershipAndAccountAsync(userId, familyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var createEnvelopeResponse = await client.PostAsJsonAsync(
            "/api/v1/envelopes",
            new CreateEnvelopeRequest(familyId, "Projection Utilities", 140m));
        Assert.Equal(HttpStatusCode.Created, createEnvelopeResponse.StatusCode);

        var createTransactionResponse = await client.PostAsJsonAsync(
            "/api/v1/transactions",
            new CreateTransactionRequest(
                accountId,
                -55m,
                "Utilities",
                "Dragon Utility",
                new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero),
                "Bills",
                EnvelopeId: null,
                Splits: null));
        Assert.Equal(HttpStatusCode.Created, createTransactionResponse.StatusCode);

        var envelopeReplayResponse = await client.PostAsync(
            $"/api/v1/reports/projections/replay?familyId={familyId}&projectionSet=EnvelopeBalances&batchSize=500&resetState=true",
            content: null);
        var envelopeReplayPayload = await envelopeReplayResponse.Content.ReadFromJsonAsync<ReportingProjectionReplayResponse>();

        Assert.Equal(HttpStatusCode.OK, envelopeReplayResponse.StatusCode);
        Assert.NotNull(envelopeReplayPayload);
        Assert.Equal("EnvelopeBalances", envelopeReplayPayload!.ProjectionSet);
        Assert.True(envelopeReplayPayload.EnvelopeProjectionRowCount >= 1);
        Assert.Equal(0, envelopeReplayPayload.TransactionProjectionRowCount);

        var futureDryRunResponse = await client.PostAsync(
            $"/api/v1/reports/projections/replay?familyId={familyId}&projectionSet=Transactions&dryRun=true&fromOccurredAtUtc=2100-01-01T00:00:00Z&toOccurredAtUtc=2100-01-02T00:00:00Z",
            content: null);
        var futureDryRunPayload = await futureDryRunResponse.Content.ReadFromJsonAsync<ReportingProjectionReplayResponse>();

        Assert.Equal(HttpStatusCode.OK, futureDryRunResponse.StatusCode);
        Assert.NotNull(futureDryRunPayload);
        Assert.Equal(0, futureDryRunPayload!.TargetedEventCount);
        Assert.Equal(0, futureDryRunPayload.ProcessedEventCount);
        Assert.Equal("Transactions", futureDryRunPayload.ProjectionSet);

        var transactionReplayResponse = await client.PostAsync(
            $"/api/v1/reports/projections/replay?familyId={familyId}&projectionSet=Transactions&resetState=false",
            content: null);
        var transactionReplayPayload = await transactionReplayResponse.Content.ReadFromJsonAsync<ReportingProjectionReplayResponse>();

        Assert.Equal(HttpStatusCode.OK, transactionReplayResponse.StatusCode);
        Assert.NotNull(transactionReplayPayload);
        Assert.Equal("Transactions", transactionReplayPayload!.ProjectionSet);
        Assert.True(transactionReplayPayload.TransactionProjectionRowCount >= 1);
    }

    [Fact]
    public async Task Replay_Applies_Throughput_Safeguards_And_MaxEvents_Cap()
    {
        var userId = "ledger-projection-user-e";
        var familyId = Guid.Parse("e1400000-0000-0000-0000-000000000041");
        var otherFamilyId = Guid.Parse("e1400000-0000-0000-0000-000000000042");

        using var client = _factory.CreateClient();
        var accountId = await SeedFamilyMembershipAndAccountAsync(userId, familyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var createEnvelopeResponse = await client.PostAsJsonAsync(
            "/api/v1/envelopes",
            new CreateEnvelopeRequest(familyId, "Projection Safeguards", 160m));
        Assert.Equal(HttpStatusCode.Created, createEnvelopeResponse.StatusCode);

        var firstTransactionResponse = await client.PostAsJsonAsync(
            "/api/v1/transactions",
            new CreateTransactionRequest(
                accountId,
                -20m,
                "Snacks",
                "Dragon Mart",
                new DateTimeOffset(2026, 2, 2, 12, 0, 0, TimeSpan.Zero),
                "Food",
                EnvelopeId: null,
                Splits: null));
        Assert.Equal(HttpStatusCode.Created, firstTransactionResponse.StatusCode);

        var secondTransactionResponse = await client.PostAsJsonAsync(
            "/api/v1/transactions",
            new CreateTransactionRequest(
                accountId,
                -30m,
                "Parking",
                "Dragon Parking",
                new DateTimeOffset(2026, 2, 3, 12, 0, 0, TimeSpan.Zero),
                "Transport",
                EnvelopeId: null,
                Splits: null));
        Assert.Equal(HttpStatusCode.Created, secondTransactionResponse.StatusCode);

        var replayResponse = await client.PostAsync(
            $"/api/v1/reports/projections/replay?familyId={familyId}&batchSize=9999&maxEvents=1&throttleMilliseconds=999999&resetState=true",
            content: null);
        var replayPayload = await replayResponse.Content.ReadFromJsonAsync<ReportingProjectionReplayResponse>();

        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.NotNull(replayPayload);
        Assert.Equal(2000, replayPayload!.BatchSize);
        Assert.Equal(1, replayPayload.MaxEvents);
        Assert.Equal(5000, replayPayload.ThrottleMilliseconds);
        Assert.Equal(1, replayPayload.TargetedEventCount);
        Assert.True(replayPayload.WasCappedByMaxEvents);
    }

    private async Task<Guid> SeedFamilyMembershipAndAccountAsync(
        string userId,
        Guid ownFamilyId,
        Guid otherFamilyId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        dbContext.ReportProjectionAppliedEvents.RemoveRange(dbContext.ReportProjectionAppliedEvents);
        dbContext.ReportProjectionReplayRuns.RemoveRange(dbContext.ReportProjectionReplayRuns);
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
