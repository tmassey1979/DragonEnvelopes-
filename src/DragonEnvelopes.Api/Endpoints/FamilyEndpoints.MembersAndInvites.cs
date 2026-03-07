using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Api.Services;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Infrastructure.Persistence;

namespace DragonEnvelopes.Api.Endpoints;

internal static partial class FamilyEndpoints
{
    private static void MapFamilyMembershipAndInviteEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPost("/families/{familyId:guid}/members", async (
                Guid familyId,
                AddFamilyMemberRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFamilyService familyService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

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
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
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

        v1.MapPost("/families/{familyId:guid}/invites/{inviteId:guid}/resend", async (
                Guid familyId,
                Guid inviteId,
                ResendFamilyInviteRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFamilyInviteService familyInviteService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var result = await familyInviteService.ResendAsync(
                    inviteId,
                    request.ExpiresInHours,
                    cancellationToken);

                return Results.Ok(new CreateFamilyInviteResponse(
                    EndpointMappers.MapFamilyInviteResponse(result.Invite),
                    result.InviteToken));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ResendFamilyInvite")
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

        v1.MapPost("/families/invites/redeem", async (
                RedeemFamilyInviteRequest request,
                ClaimsPrincipal user,
                IFamilyInviteService familyInviteService,
                CancellationToken cancellationToken) =>
            {
                var keycloakUserId = user.FindFirstValue("sub");
                if (string.IsNullOrWhiteSpace(keycloakUserId))
                {
                    return Results.Unauthorized();
                }

                var memberName = user.FindFirstValue("name")
                    ?? user.FindFirstValue("preferred_username")
                    ?? request.MemberName;
                var memberEmail = user.FindFirstValue(ClaimTypes.Email)
                    ?? user.FindFirstValue("email")
                    ?? request.MemberEmail;

                var redemption = await familyInviteService.RedeemAsync(
                    request.InviteToken,
                    keycloakUserId,
                    memberName,
                    memberEmail,
                    cancellationToken);

                return Results.Ok(EndpointMappers.MapRedeemFamilyInviteResponse(redemption));
            })
            .RequireAuthorization()
            .WithName("RedeemFamilyInvite")
            .WithOpenApi();

        v1.MapPost("/families/invites/register", async (
                RegisterFamilyInviteAccountRequest request,
                IKeycloakProvisioningService keycloakProvisioningService,
                IFamilyInviteService familyInviteService,
                CancellationToken cancellationToken) =>
            {
                var keycloakUserId = await keycloakProvisioningService.CreateUserAsync(
                    request.Email,
                    request.FirstName,
                    request.LastName,
                    request.Password,
                    cancellationToken);

                try
                {
                    var redemption = await familyInviteService.RedeemAsync(
                        request.InviteToken,
                        keycloakUserId,
                        $"{request.FirstName} {request.LastName}".Trim(),
                        request.Email,
                        cancellationToken);

                    await keycloakProvisioningService.AssignRealmRoleAsync(
                        keycloakUserId,
                        redemption.Member.Role,
                        cancellationToken);

                    return Results.Ok(new RegisterFamilyInviteAccountResponse(
                        EndpointMappers.MapFamilyInviteResponse(redemption.Invite),
                        EndpointMappers.MapFamilyMemberResponse(redemption.Member),
                        redemption.CreatedNewMember));
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
            .WithName("RegisterFamilyInviteAccount")
            .WithOpenApi();

    }
}
