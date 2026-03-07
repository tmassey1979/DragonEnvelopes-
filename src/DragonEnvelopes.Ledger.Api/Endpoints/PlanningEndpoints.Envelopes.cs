using System.Security.Claims;
using DragonEnvelopes.Application.Cqrs;
using DragonEnvelopes.Application.Cqrs.Planning;
using DragonEnvelopes.Contracts.Envelopes;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Ledger.Api.CrossCutting.Auth;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Ledger.Api.Endpoints;

internal static partial class PlanningEndpoints
{
    private static void MapEnvelopePlanningEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPost("/envelopes", async (
                CreateEnvelopeRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ICommandBus commandBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var envelope = await commandBus.SendAsync(
                    new CreateEnvelopeCommand(
                        request.FamilyId,
                        request.Name,
                        request.MonthlyBudget,
                        request.RolloverMode,
                        request.RolloverCap),
                    cancellationToken);
                return Results.Created($"/api/v1/envelopes/{envelope.Id}", EndpointMappers.MapEnvelopeResponse(envelope));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreateEnvelope")
            .WithOpenApi();

        v1.MapGet("/envelopes/{envelopeId:guid}", async (
                Guid envelopeId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IQueryBus queryBus,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.Envelopes
                    .AsNoTracking()
                    .Where(x => x.Id == envelopeId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var envelope = await queryBus.QueryAsync(
                    new GetEnvelopeByIdQuery(envelopeId),
                    cancellationToken);
                return envelope is null
                    ? Results.NotFound()
                    : Results.Ok(EndpointMappers.MapEnvelopeResponse(envelope));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetEnvelopeById")
            .WithOpenApi();

        v1.MapGet("/envelopes", async (
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

                var envelopes = await queryBus.QueryAsync(
                    new ListEnvelopesByFamilyQuery(familyId),
                    cancellationToken);
                return Results.Ok(envelopes.Select(EndpointMappers.MapEnvelopeResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListEnvelopes")
            .WithOpenApi();

        v1.MapPut("/envelopes/{envelopeId:guid}", async (
                Guid envelopeId,
                UpdateEnvelopeRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ICommandBus commandBus,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.Envelopes
                    .AsNoTracking()
                    .Where(x => x.Id == envelopeId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var envelope = await commandBus.SendAsync(
                    new UpdateEnvelopeCommand(
                        envelopeId,
                        request.Name,
                        request.MonthlyBudget,
                        request.IsArchived,
                        request.RolloverMode,
                        request.RolloverCap),
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapEnvelopeResponse(envelope));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpdateEnvelope")
            .WithOpenApi();

        v1.MapPut("/envelopes/{envelopeId:guid}/rollover-policy", async (
                Guid envelopeId,
                UpdateEnvelopeRolloverPolicyRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ICommandBus commandBus,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.Envelopes
                    .AsNoTracking()
                    .Where(x => x.Id == envelopeId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var envelope = await commandBus.SendAsync(
                    new UpdateEnvelopeRolloverPolicyCommand(
                        envelopeId,
                        request.RolloverMode,
                        request.RolloverCap),
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapEnvelopeResponse(envelope));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpdateEnvelopeRolloverPolicy")
            .WithOpenApi();

        v1.MapPost("/envelopes/{envelopeId:guid}/archive", async (
                Guid envelopeId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ICommandBus commandBus,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.Envelopes
                    .AsNoTracking()
                    .Where(x => x.Id == envelopeId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var envelope = await commandBus.SendAsync(
                    new ArchiveEnvelopeCommand(envelopeId),
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapEnvelopeResponse(envelope));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ArchiveEnvelope")
            .WithOpenApi();
    }
}
