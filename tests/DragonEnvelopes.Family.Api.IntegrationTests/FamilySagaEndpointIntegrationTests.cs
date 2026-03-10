using System.Net;
using System.Net.Http.Json;
using DragonEnvelopes.Contracts.Onboarding;
using DragonEnvelopes.Contracts.Sagas;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DragonEnvelopes.Family.Api.IntegrationTests;

public sealed class FamilySagaEndpointIntegrationTests : IClassFixture<FamilyApiFactory>
{
    private readonly FamilyApiFactory _factory;

    public FamilySagaEndpointIntegrationTests(FamilyApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OnboardingBootstrap_Creates_OnboardingSaga_And_Timeline_IsQueryable()
    {
        var userId = "family-saga-user-a";
        var ownFamilyId = Guid.Parse("d4000000-0000-0000-0000-000000000001");
        var otherFamilyId = Guid.Parse("d4000000-0000-0000-0000-000000000002");

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var bootstrapResponse = await client.PostAsJsonAsync(
            $"/api/v1/families/{ownFamilyId}/onboarding/bootstrap",
            new OnboardingBootstrapRequest(
                [new OnboardingBootstrapAccountRequest("Saga Checking", "Checking", 1250m)],
                [new OnboardingBootstrapEnvelopeRequest("Saga Groceries", 350m)],
                new OnboardingBootstrapBudgetRequest("2026-03", 4200m)));
        Assert.Equal(HttpStatusCode.OK, bootstrapResponse.StatusCode);

        var sagasResponse = await client.GetAsync($"/api/v1/families/{ownFamilyId}/sagas?workflowType=Onboarding&take=20");
        var sagasPayload = await sagasResponse.Content.ReadFromJsonAsync<List<WorkflowSagaResponse>>();
        Assert.Equal(HttpStatusCode.OK, sagasResponse.StatusCode);
        Assert.NotNull(sagasPayload);

        var onboardingSaga = Assert.Single(sagasPayload!);
        Assert.Equal("Onboarding", onboardingSaga.WorkflowType);
        Assert.Equal("PlanningBootstrapCompleted", onboardingSaga.CurrentStep);
        Assert.Equal("Running", onboardingSaga.Status);

        var timelineResponse = await client.GetAsync(
            $"/api/v1/families/{ownFamilyId}/sagas/{onboardingSaga.Id}/timeline?take=50");
        var timelinePayload = await timelineResponse.Content.ReadFromJsonAsync<List<WorkflowSagaTimelineEventResponse>>();
        Assert.Equal(HttpStatusCode.OK, timelineResponse.StatusCode);
        Assert.NotNull(timelinePayload);
        Assert.Contains(timelinePayload!, evt => evt.Step == "PlanningBootstrapRequested");
        Assert.Contains(timelinePayload!, evt => evt.Step == "PlanningBootstrapCompleted");
    }

    [Fact]
    public async Task Saga_Endpoints_Enforce_Family_Access()
    {
        var userId = "family-saga-user-b";
        var ownFamilyId = Guid.Parse("d4000000-0000-0000-0000-000000000011");
        var otherFamilyId = Guid.Parse("d4000000-0000-0000-0000-000000000012");

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAsync(userId, ownFamilyId, otherFamilyId);
        var otherSagaId = await SeedSagaAsync(otherFamilyId, "Onboarding", "onboarding:forbidden-family");
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var listForbiddenResponse = await client.GetAsync($"/api/v1/families/{otherFamilyId}/sagas?take=20");
        var getForbiddenResponse = await client.GetAsync($"/api/v1/families/{otherFamilyId}/sagas/{otherSagaId}");
        var timelineForbiddenResponse = await client.GetAsync(
            $"/api/v1/families/{otherFamilyId}/sagas/{otherSagaId}/timeline?take=20");

        Assert.Equal(HttpStatusCode.Forbidden, listForbiddenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, getForbiddenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, timelineForbiddenResponse.StatusCode);
    }

    private async Task SeedFamilyMembershipAsync(string userId, Guid ownFamilyId, Guid otherFamilyId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        dbContext.WorkflowSagaTimelineEvents.RemoveRange(dbContext.WorkflowSagaTimelineEvents);
        dbContext.WorkflowSagas.RemoveRange(dbContext.WorkflowSagas);
        dbContext.OnboardingProfiles.RemoveRange(dbContext.OnboardingProfiles);
        dbContext.Budgets.RemoveRange(dbContext.Budgets);
        dbContext.Envelopes.RemoveRange(dbContext.Envelopes);
        dbContext.Accounts.RemoveRange(dbContext.Accounts);
        dbContext.FamilyMembers.RemoveRange(dbContext.FamilyMembers);
        dbContext.Families.RemoveRange(dbContext.Families);

        var now = DateTimeOffset.UtcNow;
        dbContext.Families.AddRange(
            new DragonEnvelopes.Domain.Entities.Family(ownFamilyId, "Saga Authorized Family", now),
            new DragonEnvelopes.Domain.Entities.Family(otherFamilyId, "Saga Forbidden Family", now));

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
