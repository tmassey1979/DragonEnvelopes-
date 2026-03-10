using System.Security.Claims;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Sagas;
using DragonEnvelopes.Family.Api.CrossCutting.Auth;
using DragonEnvelopes.Infrastructure.Persistence;

namespace DragonEnvelopes.Family.Api.Endpoints;

internal static partial class FamilyEndpoints
{
    private static RouteGroupBuilder MapFamilySagaEndpoints(this RouteGroupBuilder v1)
    {
        v1.MapGet("/families/{familyId:guid}/sagas", async (
                Guid familyId,
                string? workflowType,
                int? take,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ISagaOrchestrationService sagaOrchestrationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var sagas = await sagaOrchestrationService.ListByFamilyAsync(
                    familyId,
                    workflowType,
                    take ?? 50,
                    cancellationToken);
                return Results.Ok(sagas.Select(static saga => new WorkflowSagaResponse(
                    saga.Id,
                    saga.FamilyId,
                    saga.WorkflowType,
                    saga.CorrelationId,
                    saga.ReferenceId,
                    saga.Status,
                    saga.CurrentStep,
                    saga.FailureReason,
                    saga.CompensationAction,
                    saga.StartedAtUtc,
                    saga.UpdatedAtUtc,
                    saga.CompletedAtUtc)));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListFamilyWorkflowSagas")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/sagas/{sagaId:guid}", async (
                Guid familyId,
                Guid sagaId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ISagaOrchestrationService sagaOrchestrationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var saga = await sagaOrchestrationService.GetByIdAsync(sagaId, cancellationToken);
                if (saga is null || saga.FamilyId != familyId)
                {
                    return Results.NotFound();
                }

                return Results.Ok(new WorkflowSagaResponse(
                    saga.Id,
                    saga.FamilyId,
                    saga.WorkflowType,
                    saga.CorrelationId,
                    saga.ReferenceId,
                    saga.Status,
                    saga.CurrentStep,
                    saga.FailureReason,
                    saga.CompensationAction,
                    saga.StartedAtUtc,
                    saga.UpdatedAtUtc,
                    saga.CompletedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetFamilyWorkflowSaga")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/sagas/{sagaId:guid}/timeline", async (
                Guid familyId,
                Guid sagaId,
                int? take,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ISagaOrchestrationService sagaOrchestrationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var saga = await sagaOrchestrationService.GetByIdAsync(sagaId, cancellationToken);
                if (saga is null || saga.FamilyId != familyId)
                {
                    return Results.NotFound();
                }

                var timeline = await sagaOrchestrationService.ListTimelineAsync(
                    sagaId,
                    take ?? 50,
                    cancellationToken);
                return Results.Ok(timeline.Select(static evt => new WorkflowSagaTimelineEventResponse(
                    evt.Id,
                    evt.SagaId,
                    evt.FamilyId,
                    evt.WorkflowType,
                    evt.Step,
                    evt.EventType,
                    evt.Status,
                    evt.Message,
                    evt.OccurredAtUtc)));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListFamilyWorkflowSagaTimeline")
            .WithOpenApi();

        return v1;
    }
}
