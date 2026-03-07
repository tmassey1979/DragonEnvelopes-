using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Onboarding;
using DragonEnvelopes.Infrastructure.Persistence;

namespace DragonEnvelopes.Api.Endpoints;

internal static partial class FamilyEndpoints
{
    private static void MapFamilyOnboardingEndpoints(RouteGroupBuilder v1)
    {
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
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var onboarding = await onboardingProfileService.ReconcileAsync(familyId, cancellationToken);
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
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var result = await onboardingBootstrapService.BootstrapAsync(
                    familyId,
                    request.Accounts.Select(static account => (account.Name, account.Type, account.OpeningBalance)).ToArray(),
                    request.Envelopes.Select(static envelope => (envelope.Name, envelope.MonthlyBudget)).ToArray(),
                    request.Budget is null
                        ? null
                        : (request.Budget.Month, request.Budget.TotalIncome),
                    cancellationToken);

                return Results.Ok(new OnboardingBootstrapResponse(
                    result.FamilyId,
                    result.AccountsCreated,
                    result.EnvelopesCreated,
                    result.BudgetCreated));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("BootstrapFamilyOnboarding")
            .WithOpenApi();
    }
}
