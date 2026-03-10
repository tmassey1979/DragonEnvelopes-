using System.Security.Claims;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Contracts.Onboarding;
using DragonEnvelopes.Family.Api.CrossCutting.Auth;
using DragonEnvelopes.Family.Api.Services;
using DragonEnvelopes.Infrastructure.Persistence;

namespace DragonEnvelopes.Family.Api.Endpoints;

internal static partial class FamilyEndpoints
{
    private static RouteGroupBuilder MapFamilyOnboardingEndpoints(this RouteGroupBuilder v1)
    {
        v1.MapPost("/families/onboard", async (
                CompleteFamilyOnboardingRequest request,
                IFamilyService familyService,
                IKeycloakProvisioningService keycloakProvisioningService,
                ISagaOrchestrationService sagaOrchestrationService,
                CancellationToken cancellationToken) =>
            {
                var saga = await sagaOrchestrationService.StartOrGetAsync(
                    WorkflowSagaTypes.Onboarding,
                    familyId: null,
                    correlationId: $"onboarding:{request.Email.Trim().ToLowerInvariant()}",
                    referenceId: request.Email,
                    initialStep: "OnboardingRequested",
                    message: "Family onboarding request accepted.",
                    cancellationToken);
                string? keycloakUserId = null;

                try
                {
                    keycloakUserId = await keycloakProvisioningService.CreateUserAsync(
                        request.Email,
                        request.PrimaryGuardianFirstName,
                        request.PrimaryGuardianLastName,
                        request.Password,
                        cancellationToken);
                    await sagaOrchestrationService.RecordAsync(
                        saga.Id,
                        step: "IdentityUserCreated",
                        eventType: "StepCompleted",
                        status: WorkflowSagaStatuses.Running,
                        message: $"Created identity user '{keycloakUserId}'.",
                        failureReason: null,
                        compensationAction: null,
                        markCompleted: false,
                        cancellationToken);

                    await keycloakProvisioningService.AssignRealmRoleAsync(
                        keycloakUserId,
                        "Parent",
                        cancellationToken);
                    await sagaOrchestrationService.RecordAsync(
                        saga.Id,
                        step: "IdentityRoleAssigned",
                        eventType: "StepCompleted",
                        status: WorkflowSagaStatuses.Running,
                        message: "Assigned Parent realm role.",
                        failureReason: null,
                        compensationAction: null,
                        markCompleted: false,
                        cancellationToken);

                    var family = await familyService.CreateAsync(request.FamilyName, cancellationToken);
                    saga = await sagaOrchestrationService.AssignFamilyAsync(
                        saga.Id,
                        family.Id,
                        step: "FamilyCreated",
                        eventType: "StepCompleted",
                        message: $"Created family '{family.Id}'.",
                        cancellationToken);
                    var guardianDisplayName = $"{request.PrimaryGuardianFirstName} {request.PrimaryGuardianLastName}".Trim();
                    await familyService.AddMemberAsync(
                        family.Id,
                        keycloakUserId,
                        guardianDisplayName,
                        request.Email,
                        "Parent",
                        cancellationToken);
                    await sagaOrchestrationService.RecordAsync(
                        saga.Id,
                        step: "PrimaryGuardianAdded",
                        eventType: "StepCompleted",
                        status: WorkflowSagaStatuses.Running,
                        message: "Added primary guardian to family.",
                        failureReason: null,
                        compensationAction: null,
                        markCompleted: false,
                        cancellationToken);
                    await sagaOrchestrationService.RecordAsync(
                        saga.Id,
                        step: "AwaitingPlanningAndFinancialSetup",
                        eventType: "StepCompleted",
                        status: WorkflowSagaStatuses.Running,
                        message: "Family setup complete. Waiting on planning and financial onboarding milestones.",
                        failureReason: null,
                        compensationAction: null,
                        markCompleted: false,
                        cancellationToken);

                    return Results.Created($"/api/v1/families/{family.Id}", EndpointMappers.MapFamilyResponse(family));
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrWhiteSpace(keycloakUserId))
                    {
                        try
                        {
                            await keycloakProvisioningService.DeleteUserAsync(keycloakUserId, cancellationToken);
                            await sagaOrchestrationService.RecordAsync(
                                saga.Id,
                                step: "IdentityCompensated",
                                eventType: "Compensation",
                                status: WorkflowSagaStatuses.Compensated,
                                message: $"Compensation succeeded by deleting identity user '{keycloakUserId}'.",
                                failureReason: null,
                                compensationAction: "DeleteOrphanedIdentityUser",
                                markCompleted: false,
                                cancellationToken);
                        }
                        catch (Exception compensationEx)
                        {
                            await sagaOrchestrationService.RecordAsync(
                                saga.Id,
                                step: "IdentityCompensationFailed",
                                eventType: "CompensationFailed",
                                status: WorkflowSagaStatuses.Failed,
                                message: $"Compensation failed: {compensationEx.Message}",
                                failureReason: compensationEx.Message,
                                compensationAction: "DeleteOrphanedIdentityUser",
                                markCompleted: false,
                                cancellationToken);
                        }
                    }

                    await sagaOrchestrationService.RecordAsync(
                        saga.Id,
                        step: "OnboardingFailed",
                        eventType: "StepFailed",
                        status: WorkflowSagaStatuses.Failed,
                        message: ex.Message,
                        failureReason: ex.Message,
                        compensationAction: "DeleteOrphanedIdentityUser",
                        markCompleted: false,
                        cancellationToken);
                    throw;
                }
            })
            .AllowAnonymous()
            .WithName("CompleteFamilyOnboarding")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/onboarding", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IOnboardingProfileService onboardingProfileService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var onboarding = await onboardingProfileService.GetOrCreateAsync(familyId, cancellationToken);
                return Results.Ok(EndpointMappers.MapOnboardingProfileResponse(onboarding));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetFamilyOnboardingProfile")
            .WithOpenApi();

        v1.MapPut("/families/{familyId:guid}/onboarding", async (
                Guid familyId,
                UpdateOnboardingProfileRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IOnboardingProfileService onboardingProfileService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var onboarding = await onboardingProfileService.UpdateAsync(
                    familyId,
                    request.MembersCompleted,
                    request.AccountsCompleted,
                    request.EnvelopesCompleted,
                    request.BudgetCompleted,
                    request.PlaidCompleted,
                    request.StripeAccountsCompleted,
                    request.CardsCompleted,
                    request.AutomationCompleted,
                    cancellationToken);

                return Results.Ok(EndpointMappers.MapOnboardingProfileResponse(onboarding));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpdateFamilyOnboardingProfile")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/onboarding/reconcile", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IOnboardingProfileService onboardingProfileService,
                ISagaOrchestrationService sagaOrchestrationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var onboarding = await onboardingProfileService.ReconcileAsync(familyId, cancellationToken);
                var saga = await sagaOrchestrationService.GetLatestByFamilyAndWorkflowAsync(
                    familyId,
                    WorkflowSagaTypes.Onboarding,
                    cancellationToken);
                if (saga is not null)
                {
                    var step = onboarding.IsCompleted
                        ? "OnboardingCompleted"
                        : "OnboardingReconciled";
                    var status = onboarding.IsCompleted
                        ? WorkflowSagaStatuses.Completed
                        : WorkflowSagaStatuses.Running;
                    var summary =
                        $"Members={onboarding.MembersCompleted}, Accounts={onboarding.AccountsCompleted}, Envelopes={onboarding.EnvelopesCompleted}, Budget={onboarding.BudgetCompleted}, Plaid={onboarding.PlaidCompleted}, StripeAccounts={onboarding.StripeAccountsCompleted}, Cards={onboarding.CardsCompleted}, Automation={onboarding.AutomationCompleted}.";
                    await sagaOrchestrationService.RecordAsync(
                        saga.Id,
                        step,
                        eventType: "StepCompleted",
                        status,
                        message: summary,
                        failureReason: null,
                        compensationAction: null,
                        markCompleted: onboarding.IsCompleted,
                        cancellationToken);
                }
                return Results.Ok(EndpointMappers.MapOnboardingProfileResponse(onboarding));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ReconcileFamilyOnboardingProfile")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/onboarding/bootstrap", async (
                Guid familyId,
                OnboardingBootstrapRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IOnboardingBootstrapService onboardingBootstrapService,
                ISagaOrchestrationService sagaOrchestrationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var saga = await sagaOrchestrationService.StartOrGetAsync(
                    WorkflowSagaTypes.Onboarding,
                    familyId,
                    correlationId: $"onboarding:{familyId:D}",
                    referenceId: familyId.ToString("D"),
                    initialStep: "PlanningBootstrapRequested",
                    message: "Onboarding bootstrap requested.",
                    cancellationToken);

                var result = default((Guid FamilyId, int AccountsCreated, int EnvelopesCreated, bool BudgetCreated)?);
                try
                {
                    var bootstrap = await onboardingBootstrapService.BootstrapAsync(
                        familyId,
                        request.Accounts.Select(static account => (account.Name, account.Type, account.OpeningBalance)).ToArray(),
                        request.Envelopes.Select(static envelope => (envelope.Name, envelope.MonthlyBudget)).ToArray(),
                        request.Budget is null
                            ? null
                            : (request.Budget.Month, request.Budget.TotalIncome),
                        cancellationToken);
                    result = (bootstrap.FamilyId, bootstrap.AccountsCreated, bootstrap.EnvelopesCreated, bootstrap.BudgetCreated);
                }
                catch (Exception ex)
                {
                    await sagaOrchestrationService.RecordAsync(
                        saga.Id,
                        step: "PlanningBootstrapFailed",
                        eventType: "StepFailed",
                        status: WorkflowSagaStatuses.Failed,
                        message: ex.Message,
                        failureReason: ex.Message,
                        compensationAction: "ManualBootstrapRollbackReview",
                        markCompleted: false,
                        cancellationToken);
                    throw;
                }

                await sagaOrchestrationService.RecordAsync(
                    saga.Id,
                    step: "PlanningBootstrapCompleted",
                    eventType: "StepCompleted",
                    status: WorkflowSagaStatuses.Running,
                    message:
                    $"AccountsCreated={result!.Value.AccountsCreated}, EnvelopesCreated={result.Value.EnvelopesCreated}, BudgetCreated={result.Value.BudgetCreated}.",
                    failureReason: null,
                    compensationAction: null,
                    markCompleted: false,
                    cancellationToken);

                return Results.Ok(new OnboardingBootstrapResponse(
                    result!.Value.FamilyId,
                    result.Value.AccountsCreated,
                    result.Value.EnvelopesCreated,
                    result.Value.BudgetCreated));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("BootstrapFamilyOnboarding")
            .WithOpenApi();

        return v1;
    }
}

