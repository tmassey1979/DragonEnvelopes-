using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Api.Services;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Contracts.Onboarding;
using DragonEnvelopes.Infrastructure.Persistence;

namespace DragonEnvelopes.Api.Endpoints;

internal static partial class FamilyEndpoints
{
    private static void MapFamilyLifecycleEndpoints(RouteGroupBuilder v1)
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

        v1.MapGet("/families/{familyId:guid}/profile", async (
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

                var profile = await familyService.GetProfileAsync(familyId, cancellationToken);
                return profile is null
                    ? Results.NotFound()
                    : Results.Ok(EndpointMappers.MapFamilyProfileResponse(profile));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetFamilyProfile")
            .WithOpenApi();

        v1.MapPut("/families/{familyId:guid}/profile", async (
                Guid familyId,
                UpdateFamilyProfileRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFamilyService familyService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var profile = await familyService.UpdateProfileAsync(
                    familyId,
                    request.Name,
                    request.CurrencyCode,
                    request.TimeZoneId,
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapFamilyProfileResponse(profile));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpdateFamilyProfile")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/budget-preferences", async (
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

                var preferences = await familyService.GetBudgetPreferencesAsync(familyId, cancellationToken);
                return preferences is null
                    ? Results.NotFound()
                    : Results.Ok(EndpointMappers.MapFamilyBudgetPreferencesResponse(preferences));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetFamilyBudgetPreferences")
            .WithOpenApi();

        v1.MapPut("/families/{familyId:guid}/budget-preferences", async (
                Guid familyId,
                UpdateFamilyBudgetPreferencesRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFamilyService familyService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var preferences = await familyService.UpdateBudgetPreferencesAsync(
                    familyId,
                    request.PayFrequency,
                    request.BudgetingStyle,
                    request.HouseholdMonthlyIncome,
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapFamilyBudgetPreferencesResponse(preferences));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpdateFamilyBudgetPreferences")
            .WithOpenApi();

    }
}
