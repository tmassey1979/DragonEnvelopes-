using System.Security.Claims;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Approvals;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Ledger.Api.CrossCutting.Auth;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Ledger.Api.Endpoints;

internal static partial class PlanningEndpoints
{
    private static void MapApprovalPlanningEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPut("/approvals/policy", async (
                UpsertApprovalPolicyRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IApprovalWorkflowService approvalWorkflowService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var policy = await approvalWorkflowService.UpsertPolicyAsync(
                    request.FamilyId,
                    request.IsEnabled,
                    request.AmountThreshold,
                    request.RolesRequiringApproval,
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapApprovalPolicyResponse(policy));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("UpsertApprovalPolicy")
            .WithOpenApi();

        v1.MapGet("/approvals/policy", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IApprovalWorkflowService approvalWorkflowService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var policy = await approvalWorkflowService.GetPolicyAsync(familyId, cancellationToken);
                return policy is null
                    ? Results.NotFound()
                    : Results.Ok(EndpointMappers.MapApprovalPolicyResponse(policy));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetApprovalPolicy")
            .WithOpenApi();

        v1.MapPost("/approvals/requests", async (
                CreateApprovalRequestRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IApprovalWorkflowService approvalWorkflowService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var userId = user.FindFirstValue("sub");
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Results.Forbid();
                }

                var memberRole = await dbContext.FamilyMembers
                    .AsNoTracking()
                    .Where(member => member.FamilyId == request.FamilyId && member.KeycloakUserId == userId)
                    .Select(member => (string?)member.Role.ToString())
                    .FirstOrDefaultAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(memberRole))
                {
                    return Results.Forbid();
                }

                var created = await approvalWorkflowService.CreateRequestAsync(
                    request.FamilyId,
                    request.AccountId,
                    userId,
                    memberRole,
                    request.Amount,
                    request.Description,
                    request.Merchant,
                    request.OccurredAt,
                    request.Category,
                    request.EnvelopeId,
                    request.Notes,
                    cancellationToken);
                return Results.Created(
                    $"/api/v1/approvals/requests/{created.Id}",
                    EndpointMappers.MapApprovalRequestResponse(created));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreateApprovalRequest")
            .WithOpenApi();

        v1.MapGet("/approvals/requests", async (
                Guid familyId,
                string? status,
                int? take,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IApprovalWorkflowService approvalWorkflowService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var requests = await approvalWorkflowService.ListRequestsAsync(
                    familyId,
                    status,
                    take ?? 50,
                    cancellationToken);
                return Results.Ok(requests.Select(EndpointMappers.MapApprovalRequestResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListApprovalRequests")
            .WithOpenApi();

        v1.MapGet("/approvals/requests/{requestId:guid}/timeline", async (
                Guid requestId,
                int? take,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IApprovalWorkflowService approvalWorkflowService,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.PurchaseApprovalRequests
                    .AsNoTracking()
                    .Where(x => x.Id == requestId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var timeline = await approvalWorkflowService.ListTimelineAsync(
                    requestId,
                    take ?? 50,
                    cancellationToken);
                return Results.Ok(timeline.Select(EndpointMappers.MapApprovalTimelineEventResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListApprovalRequestTimeline")
            .WithOpenApi();

        v1.MapPost("/approvals/requests/{requestId:guid}/approve", async (
                Guid requestId,
                ResolveApprovalRequestRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IApprovalWorkflowService approvalWorkflowService,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.PurchaseApprovalRequests
                    .AsNoTracking()
                    .Where(x => x.Id == requestId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var userId = user.FindFirstValue("sub");
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Results.Forbid();
                }

                var memberRole = await dbContext.FamilyMembers
                    .AsNoTracking()
                    .Where(member => member.FamilyId == familyId.Value && member.KeycloakUserId == userId)
                    .Select(member => (string?)member.Role.ToString())
                    .FirstOrDefaultAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(memberRole))
                {
                    return Results.Forbid();
                }

                var resolved = await approvalWorkflowService.ApproveAsync(
                    requestId,
                    userId,
                    memberRole,
                    request.Notes,
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapApprovalRequestResponse(resolved));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("ApproveApprovalRequest")
            .WithOpenApi();

        v1.MapPost("/approvals/requests/{requestId:guid}/deny", async (
                Guid requestId,
                ResolveApprovalRequestRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IApprovalWorkflowService approvalWorkflowService,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.PurchaseApprovalRequests
                    .AsNoTracking()
                    .Where(x => x.Id == requestId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var userId = user.FindFirstValue("sub");
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Results.Forbid();
                }

                var memberRole = await dbContext.FamilyMembers
                    .AsNoTracking()
                    .Where(member => member.FamilyId == familyId.Value && member.KeycloakUserId == userId)
                    .Select(member => (string?)member.Role.ToString())
                    .FirstOrDefaultAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(memberRole))
                {
                    return Results.Forbid();
                }

                var resolved = await approvalWorkflowService.DenyAsync(
                    requestId,
                    userId,
                    memberRole,
                    request.Notes,
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapApprovalRequestResponse(resolved));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("DenyApprovalRequest")
            .WithOpenApi();
    }
}
