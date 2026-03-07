using System.Security.Claims;
using DragonEnvelopes.Application.Cqrs;
using DragonEnvelopes.Application.Cqrs.Planning;
using DragonEnvelopes.Contracts.EnvelopeGoals;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Ledger.Api.CrossCutting.Auth;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Ledger.Api.Endpoints;

internal static partial class PlanningEndpoints
{
    private static void MapEnvelopeGoalPlanningEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPost("/envelope-goals", async (
                CreateEnvelopeGoalRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ICommandBus commandBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var created = await commandBus.SendAsync(
                    new CreateEnvelopeGoalCommand(
                        request.FamilyId,
                        request.EnvelopeId,
                        request.TargetAmount,
                        request.DueDate,
                        request.Status),
                    cancellationToken);

                return Results.Created($"/api/v1/envelope-goals/{created.Id}", EndpointMappers.MapEnvelopeGoalResponse(created));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreateEnvelopeGoal")
            .WithOpenApi();

        v1.MapGet("/envelope-goals", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IQueryBus queryBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var goals = await queryBus.QueryAsync(
                    new ListEnvelopeGoalsByFamilyQuery(familyId),
                    cancellationToken);
                return Results.Ok(goals.Select(EndpointMappers.MapEnvelopeGoalResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListEnvelopeGoals")
            .WithOpenApi();

        v1.MapGet("/envelope-goals/{goalId:guid}", async (
                Guid goalId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IQueryBus queryBus,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.EnvelopeGoals
                    .AsNoTracking()
                    .Where(x => x.Id == goalId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var goal = await queryBus.QueryAsync(
                    new GetEnvelopeGoalByIdQuery(goalId),
                    cancellationToken);
                return goal is null
                    ? Results.NotFound()
                    : Results.Ok(EndpointMappers.MapEnvelopeGoalResponse(goal));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetEnvelopeGoalById")
            .WithOpenApi();

        v1.MapPut("/envelope-goals/{goalId:guid}", async (
                Guid goalId,
                UpdateEnvelopeGoalRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ICommandBus commandBus,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.EnvelopeGoals
                    .AsNoTracking()
                    .Where(x => x.Id == goalId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var updated = await commandBus.SendAsync(
                    new UpdateEnvelopeGoalCommand(
                        goalId,
                        request.TargetAmount,
                        request.DueDate,
                        request.Status),
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapEnvelopeGoalResponse(updated));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpdateEnvelopeGoal")
            .WithOpenApi();

        v1.MapDelete("/envelope-goals/{goalId:guid}", async (
                Guid goalId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ICommandBus commandBus,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.EnvelopeGoals
                    .AsNoTracking()
                    .Where(x => x.Id == goalId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                await commandBus.SendAsync(
                    new DeleteEnvelopeGoalCommand(goalId),
                    cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("DeleteEnvelopeGoal")
            .WithOpenApi();

        v1.MapGet("/envelope-goals/projection", async (
                Guid familyId,
                DateOnly? asOf,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IQueryBus queryBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var projection = await queryBus.QueryAsync(
                    new ProjectEnvelopeGoalsQuery(
                        familyId,
                        asOf ?? DateOnly.FromDateTime(DateTime.UtcNow)),
                    cancellationToken);
                return Results.Ok(projection.Select(EndpointMappers.MapEnvelopeGoalProjectionResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ProjectEnvelopeGoals")
            .WithOpenApi();
    }
}
