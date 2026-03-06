using System.Security.Claims;
using DragonEnvelopes.Family.Api.CrossCutting.Auth;
using DragonEnvelopes.Family.Api.Services;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Contracts.Onboarding;
using DragonEnvelopes.Infrastructure.Persistence;

namespace DragonEnvelopes.Family.Api.Endpoints;

internal static class FamilyEndpoints
{
    public static RouteGroupBuilder MapFamilyEndpoints(this RouteGroupBuilder v1)
    {
        v1.MapPost("/families", async (
                CreateFamilyRequest request,
                IFamilyService familyService,
                CancellationToken cancellationToken) =>
            {
                var family = await familyService.CreateAsync(request.Name, cancellationToken);
                return Results.Created($"/api/v1/families/{family.Id}", EndpointMappers.MapFamilyResponse(family));
            })
            .AllowAnonymous()
            .WithName("CreateFamily")
            .WithOpenApi();

        v1.MapPost("/families/onboard", async (
                CompleteFamilyOnboardingRequest request,
                IFamilyService familyService,
                IKeycloakProvisioningService keycloakProvisioningService,
                CancellationToken cancellationToken) =>
            {
                var keycloakUserId = await keycloakProvisioningService.CreateUserAsync(
                    request.Email,
                    request.PrimaryGuardianFirstName,
                    request.PrimaryGuardianLastName,
                    request.Password,
                    cancellationToken);
                await keycloakProvisioningService.AssignRealmRoleAsync(
                    keycloakUserId,
                    "Parent",
                    cancellationToken);

                try
                {
                    var family = await familyService.CreateAsync(request.FamilyName, cancellationToken);
                    var guardianDisplayName = $"{request.PrimaryGuardianFirstName} {request.PrimaryGuardianLastName}".Trim();
                    await familyService.AddMemberAsync(
                        family.Id,
                        keycloakUserId,
                        guardianDisplayName,
                        request.Email,
                        "Parent",
                        cancellationToken);

                    return Results.Created($"/api/v1/families/{family.Id}", EndpointMappers.MapFamilyResponse(family));
                }
                catch
                {
                    try
                    {
                        await keycloakProvisioningService.DeleteUserAsync(keycloakUserId, cancellationToken);
                    }
                    catch
                    {
                        // Best-effort compensation to avoid orphaned identity users.
                    }

                    throw;
                }
            })
            .AllowAnonymous()
            .WithName("CompleteFamilyOnboarding")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFamilyService familyService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var family = await familyService.GetByIdAsync(familyId, cancellationToken);
                return family is null
                    ? Results.NotFound()
                    : Results.Ok(EndpointMappers.MapFamilyResponse(family));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetFamilyById")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/members", async (
                Guid familyId,
                AddFamilyMemberRequest request,
                IFamilyService familyService,
                CancellationToken cancellationToken) =>
            {
                var member = await familyService.AddMemberAsync(
                    familyId,
                    request.KeycloakUserId,
                    request.Name,
                    request.Email,
                    request.Role,
                    cancellationToken);

                return Results.Created(
                    $"/api/v1/families/{familyId}/members/{member.Id}",
                    EndpointMappers.MapFamilyMemberResponse(member));
            })
            .AllowAnonymous()
            .WithName("AddFamilyMember")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/members", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFamilyService familyService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var members = await familyService.ListMembersAsync(familyId, cancellationToken);
                return members is null
                    ? Results.NotFound()
                    : Results.Ok(members.Select(EndpointMappers.MapFamilyMemberResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListFamilyMembers")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/invites", async (
                Guid familyId,
                CreateFamilyInviteRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFamilyInviteService familyInviteService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var result = await familyInviteService.CreateAsync(
                    familyId,
                    request.Email,
                    request.Role,
                    request.ExpiresInHours,
                    cancellationToken);

                return Results.Created(
                    $"/api/v1/families/{familyId}/invites/{result.Invite.Id}",
                    new CreateFamilyInviteResponse(
                        EndpointMappers.MapFamilyInviteResponse(result.Invite),
                        result.InviteToken));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreateFamilyInvite")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/invites", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFamilyInviteService familyInviteService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var invites = await familyInviteService.ListByFamilyAsync(familyId, cancellationToken);
                return Results.Ok(invites.Select(EndpointMappers.MapFamilyInviteResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListFamilyInvites")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/invites/{inviteId:guid}/cancel", async (
                Guid familyId,
                Guid inviteId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFamilyInviteService familyInviteService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var invite = await familyInviteService.CancelAsync(inviteId, cancellationToken);
                return Results.Ok(EndpointMappers.MapFamilyInviteResponse(invite));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CancelFamilyInvite")
            .WithOpenApi();

        v1.MapPost("/families/invites/accept", async (
                AcceptFamilyInviteRequest request,
                IFamilyInviteService familyInviteService,
                CancellationToken cancellationToken) =>
            {
                var invite = await familyInviteService.AcceptAsync(request.InviteToken, cancellationToken);
                return Results.Ok(EndpointMappers.MapFamilyInviteResponse(invite));
            })
            .AllowAnonymous()
            .WithName("AcceptFamilyInvite")
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

        return v1;
    }
}
