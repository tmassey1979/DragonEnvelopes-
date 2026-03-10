using System.Net;
using System.Net.Http.Json;
using DragonEnvelopes.Contracts.Approvals;
using DragonEnvelopes.Contracts.Sagas;
using DragonEnvelopes.Contracts.Transactions;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DragonEnvelopes.Ledger.Api.IntegrationTests;

public sealed class LedgerSagaEndpointIntegrationTests : IClassFixture<LedgerApiFactory>
{
    private readonly LedgerApiFactory _factory;

    public LedgerSagaEndpointIntegrationTests(LedgerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Approval_Workflow_Saga_Is_Queryable_With_Timeline()
    {
        var childUserId = "ledger-saga-child-a";
        var parentUserId = "ledger-saga-parent-a";
        var ownFamilyId = Guid.Parse("e1500000-0000-0000-0000-000000000001");
        var otherFamilyId = Guid.Parse("e1500000-0000-0000-0000-000000000002");

        using var client = _factory.CreateClient();
        var ownAccountId = await SeedFamilyMembershipAndApprovalPolicyAsync(
            childUserId,
            parentUserId,
            ownFamilyId,
            otherFamilyId);

        client.DefaultRequestHeaders.Remove("X-Test-User");
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Add("X-Test-User", childUserId);
        client.DefaultRequestHeaders.Add("X-Test-Role", "Child");

        var blockedResponse = await client.PostAsJsonAsync(
            "/api/v1/transactions",
            new CreateTransactionRequest(
                ownAccountId,
                -160m,
                "Saga test laptop bag",
                "Dragon Gear",
                DateTimeOffset.UtcNow,
                "Shopping",
                EnvelopeId: null,
                Splits: null));
        var blockedPayload = await blockedResponse.Content.ReadFromJsonAsync<ApprovalRequestResponse>();
        Assert.Equal(HttpStatusCode.Accepted, blockedResponse.StatusCode);
        Assert.NotNull(blockedPayload);

        client.DefaultRequestHeaders.Remove("X-Test-User");
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Add("X-Test-User", parentUserId);
        client.DefaultRequestHeaders.Add("X-Test-Role", "Parent");

        var approveResponse = await client.PostAsJsonAsync(
            $"/api/v1/approvals/requests/{blockedPayload!.Id}/approve",
            new ResolveApprovalRequestRequest("approved for saga test"));
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var sagasResponse = await client.GetAsync($"/api/v1/families/{ownFamilyId}/sagas?workflowType=Approval&take=20");
        var sagasPayload = await sagasResponse.Content.ReadFromJsonAsync<List<WorkflowSagaResponse>>();
        Assert.Equal(HttpStatusCode.OK, sagasResponse.StatusCode);
        Assert.NotNull(sagasPayload);

        var approvalSaga = Assert.Single(sagasPayload!);
        Assert.Equal("Approval", approvalSaga.WorkflowType);
        Assert.Equal(blockedPayload.Id.ToString("D"), approvalSaga.ReferenceId);
        Assert.Equal("ApprovalResolvedAndPosted", approvalSaga.CurrentStep);
        Assert.Equal("Completed", approvalSaga.Status);
        Assert.NotNull(approvalSaga.CompletedAtUtc);

        var timelineResponse = await client.GetAsync(
            $"/api/v1/families/{ownFamilyId}/sagas/{approvalSaga.Id}/timeline?take=50");
        var timelinePayload = await timelineResponse.Content.ReadFromJsonAsync<List<WorkflowSagaTimelineEventResponse>>();
        Assert.Equal(HttpStatusCode.OK, timelineResponse.StatusCode);
        Assert.NotNull(timelinePayload);
        Assert.Contains(timelinePayload!, evt => evt.Step == "ApprovalWorkflowInitialized");
        Assert.Contains(timelinePayload!, evt => evt.Step == "ApprovalRequestBlocked");
        Assert.Contains(timelinePayload!, evt => evt.Step == "ApprovalResolutionStarted");
        Assert.Contains(timelinePayload!, evt => evt.Step == "ApprovalResolvedAndPosted");
    }

    [Fact]
    public async Task Ledger_Saga_Endpoints_Enforce_Family_Access()
    {
        var userId = "ledger-saga-user-b";
        var ownFamilyId = Guid.Parse("e1500000-0000-0000-0000-000000000011");
        var otherFamilyId = Guid.Parse("e1500000-0000-0000-0000-000000000012");

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAsync(userId, ownFamilyId, otherFamilyId);
        var otherSagaId = await SeedSagaAsync(otherFamilyId, "Approval", "approval:forbidden-family");
        client.DefaultRequestHeaders.Add("X-Test-User", userId);
        client.DefaultRequestHeaders.Add("X-Test-Role", "Parent");

        var listForbiddenResponse = await client.GetAsync($"/api/v1/families/{otherFamilyId}/sagas?take=20");
        var getForbiddenResponse = await client.GetAsync($"/api/v1/families/{otherFamilyId}/sagas/{otherSagaId}");
        var timelineForbiddenResponse = await client.GetAsync(
            $"/api/v1/families/{otherFamilyId}/sagas/{otherSagaId}/timeline?take=20");

        Assert.Equal(HttpStatusCode.Forbidden, listForbiddenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, getForbiddenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, timelineForbiddenResponse.StatusCode);
    }

    private async Task<Guid> SeedFamilyMembershipAndApprovalPolicyAsync(
        string childUserId,
        string parentUserId,
        Guid ownFamilyId,
        Guid otherFamilyId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        dbContext.WorkflowSagaTimelineEvents.RemoveRange(dbContext.WorkflowSagaTimelineEvents);
        dbContext.WorkflowSagas.RemoveRange(dbContext.WorkflowSagas);
        dbContext.PurchaseApprovalTimelineEvents.RemoveRange(dbContext.PurchaseApprovalTimelineEvents);
        dbContext.PurchaseApprovalRequests.RemoveRange(dbContext.PurchaseApprovalRequests);
        dbContext.FamilyApprovalPolicies.RemoveRange(dbContext.FamilyApprovalPolicies);
        dbContext.TransactionSplits.RemoveRange(dbContext.TransactionSplits);
        dbContext.Transactions.RemoveRange(dbContext.Transactions);
        dbContext.Accounts.RemoveRange(dbContext.Accounts);
        dbContext.FamilyMembers.RemoveRange(dbContext.FamilyMembers);
        dbContext.Families.RemoveRange(dbContext.Families);

        var now = DateTimeOffset.UtcNow;
        var ownAccountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();

        dbContext.Families.AddRange(
            new Family(ownFamilyId, "Saga Authorized Ledger Family", now),
            new Family(otherFamilyId, "Saga Forbidden Ledger Family", now));

        dbContext.FamilyMembers.AddRange(
            new FamilyMember(
                Guid.NewGuid(),
                ownFamilyId,
                childUserId,
                "Saga Child User",
                EmailAddress.Parse("saga.child@test.local"),
                MemberRole.Child),
            new FamilyMember(
                Guid.NewGuid(),
                ownFamilyId,
                parentUserId,
                "Saga Parent User",
                EmailAddress.Parse("saga.parent@test.local"),
                MemberRole.Parent));

        dbContext.Accounts.AddRange(
            new Account(ownAccountId, ownFamilyId, "Saga Checking", AccountType.Checking, Money.FromDecimal(700m)),
            new Account(otherAccountId, otherFamilyId, "Other Checking", AccountType.Checking, Money.FromDecimal(700m)));

        dbContext.FamilyApprovalPolicies.AddRange(
            FamilyApprovalPolicy.Create(
                Guid.NewGuid(),
                ownFamilyId,
                isEnabled: true,
                amountThreshold: 100m,
                rolesRequiringApproval: ["Child"],
                updatedAtUtc: now),
            FamilyApprovalPolicy.Create(
                Guid.NewGuid(),
                otherFamilyId,
                isEnabled: true,
                amountThreshold: 100m,
                rolesRequiringApproval: ["Child"],
                updatedAtUtc: now));

        await dbContext.SaveChangesAsync();
        return ownAccountId;
    }

    private async Task SeedFamilyMembershipAsync(string userId, Guid ownFamilyId, Guid otherFamilyId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        dbContext.WorkflowSagaTimelineEvents.RemoveRange(dbContext.WorkflowSagaTimelineEvents);
        dbContext.WorkflowSagas.RemoveRange(dbContext.WorkflowSagas);
        dbContext.PurchaseApprovalTimelineEvents.RemoveRange(dbContext.PurchaseApprovalTimelineEvents);
        dbContext.PurchaseApprovalRequests.RemoveRange(dbContext.PurchaseApprovalRequests);
        dbContext.FamilyApprovalPolicies.RemoveRange(dbContext.FamilyApprovalPolicies);
        dbContext.TransactionSplits.RemoveRange(dbContext.TransactionSplits);
        dbContext.Transactions.RemoveRange(dbContext.Transactions);
        dbContext.Accounts.RemoveRange(dbContext.Accounts);
        dbContext.FamilyMembers.RemoveRange(dbContext.FamilyMembers);
        dbContext.Families.RemoveRange(dbContext.Families);

        var now = DateTimeOffset.UtcNow;
        dbContext.Families.AddRange(
            new Family(ownFamilyId, "Saga Authorized Ledger Family", now),
            new Family(otherFamilyId, "Saga Forbidden Ledger Family", now));

        dbContext.FamilyMembers.Add(new FamilyMember(
            Guid.NewGuid(),
            ownFamilyId,
            userId,
            "Saga Parent User",
            EmailAddress.Parse("saga.parent@test.local"),
            MemberRole.Parent));

        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> SeedSagaAsync(Guid familyId, string workflowType, string correlationId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        var now = DateTimeOffset.UtcNow;
        var saga = new WorkflowSaga(
            Guid.NewGuid(),
            familyId,
            workflowType,
            correlationId,
            familyId.ToString("D"),
            status: "Running",
            currentStep: "Seeded",
            failureReason: null,
            compensationAction: null,
            startedAtUtc: now,
            updatedAtUtc: now,
            completedAtUtc: null);
        var timelineEvent = new WorkflowSagaTimelineEvent(
            Guid.NewGuid(),
            saga.Id,
            familyId,
            workflowType,
            "Seeded",
            eventType: "Started",
            status: "Running",
            message: "Seeded for access-control test.",
            occurredAtUtc: now);
        dbContext.WorkflowSagas.Add(saga);
        dbContext.WorkflowSagaTimelineEvents.Add(timelineEvent);
        await dbContext.SaveChangesAsync();
        return saga.Id;
    }
}
