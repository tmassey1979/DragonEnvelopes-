using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Envelopes;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Api.Endpoints;

internal static partial class PlanningAndReportingEndpoints
{
    private static void MapEnvelopePlanningEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPost("/envelopes", async (
                CreateEnvelopeRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopeService envelopeService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var envelope = await envelopeService.CreateAsync(
                    request.FamilyId,
                    request.Name,
                    request.MonthlyBudget,
                    request.RolloverMode,
                    request.RolloverCap,
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
                IEnvelopeService envelopeService,
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

                var envelope = await envelopeService.GetByIdAsync(envelopeId, cancellationToken);
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
                IEnvelopeService envelopeService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var envelopes = await envelopeService.ListByFamilyAsync(familyId, cancellationToken);
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
                IEnvelopeService envelopeService,
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

                var envelope = await envelopeService.UpdateAsync(
                    envelopeId,
                    request.Name,
                    request.MonthlyBudget,
                    request.IsArchived,
                    request.RolloverMode,
                    request.RolloverCap,
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
                IEnvelopeService envelopeService,
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

                var envelope = await envelopeService.UpdateRolloverPolicyAsync(
                    envelopeId,
                    request.RolloverMode,
                    request.RolloverCap,
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
                IEnvelopeService envelopeService,
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

                var envelope = await envelopeService.ArchiveAsync(envelopeId, cancellationToken);
                return Results.Ok(EndpointMappers.MapEnvelopeResponse(envelope));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ArchiveEnvelope")
            .WithOpenApi();

    }
}
