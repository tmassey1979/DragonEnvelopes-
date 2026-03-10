using System.Security.Claims;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Reports;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Ledger.Api.CrossCutting.Auth;

namespace DragonEnvelopes.Ledger.Api.Endpoints;

internal static partial class PlanningEndpoints
{
    private static void MapReportingEndpoints(RouteGroupBuilder v1)
    {
        v1.MapGet("/reports/envelope-balances", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IReportingService reportingService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var result = await reportingService.GetEnvelopeBalancesAsync(familyId, cancellationToken);
                return Results.Ok(result.Select(EndpointMappers.MapEnvelopeBalanceReportResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetEnvelopeBalancesReport")
            .WithOpenApi();

        v1.MapGet("/reports/monthly-spend", async (
                Guid familyId,
                DateTimeOffset from,
                DateTimeOffset to,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IReportingService reportingService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var result = await reportingService.GetMonthlySpendAsync(familyId, from, to, cancellationToken);
                return Results.Ok(result.Select(EndpointMappers.MapMonthlySpendReportPointResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetMonthlySpendReport")
            .WithOpenApi();

        v1.MapGet("/reports/category-breakdown", async (
                Guid familyId,
                DateTimeOffset from,
                DateTimeOffset to,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IReportingService reportingService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var result = await reportingService.GetCategoryBreakdownAsync(familyId, from, to, cancellationToken);
                return Results.Ok(result.Select(EndpointMappers.MapCategoryBreakdownReportItemResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetCategoryBreakdownReport")
            .WithOpenApi();

        v1.MapGet("/reports/remaining-budget", async (
                Guid familyId,
                string month,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IReportingService reportingService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var result = await reportingService.GetRemainingBudgetAsync(familyId, month, cancellationToken);
                return result is null
                    ? Results.NotFound()
                    : Results.Ok(EndpointMappers.MapRemainingBudgetReportResponse(result));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetRemainingBudgetReport")
            .WithOpenApi();

        v1.MapGet("/reports/projections/status", async (
                Guid? familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IReportingProjectionService reportingProjectionService,
                CancellationToken cancellationToken) =>
            {
                if (familyId.HasValue
                    && !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var status = await reportingProjectionService.GetStatusAsync(familyId, cancellationToken);
                return Results.Ok(new ReportingProjectionStatusResponse(
                    status.FamilyId,
                    status.PendingCount,
                    status.AppliedCount,
                    status.FailedCount,
                    status.EnvelopeProjectionRowCount,
                    status.TransactionProjectionRowCount,
                    status.LastAppliedAtUtc,
                    status.LatestEventOccurredAtUtc,
                    status.ApproximateLagSeconds));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("GetReportingProjectionStatus")
            .WithOpenApi();

        v1.MapPost("/reports/projections/replay", async (
                Guid? familyId,
                string? projectionSet,
                DateTimeOffset? fromOccurredAtUtc,
                DateTimeOffset? toOccurredAtUtc,
                bool? dryRun,
                bool? resetState,
                int? batchSize,
                int? maxEvents,
                int? throttleMilliseconds,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IReportingProjectionService reportingProjectionService,
                CancellationToken cancellationToken) =>
            {
                if (familyId.HasValue
                    && !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var replay = await reportingProjectionService.ReplayAsync(
                    new ReportingProjectionReplayRequestDetails(
                        familyId,
                        projectionSet ?? ReportingProjectionSets.All,
                        fromOccurredAtUtc,
                        toOccurredAtUtc,
                        dryRun ?? false,
                        resetState ?? true,
                        batchSize ?? 500,
                        maxEvents ?? 50_000,
                        throttleMilliseconds ?? 0,
                        user.FindFirstValue("sub")),
                    cancellationToken);
                return Results.Ok(new ReportingProjectionReplayResponse(
                    replay.ReplayRunId,
                    replay.FamilyId,
                    replay.ProjectionSet,
                    replay.FromOccurredAtUtc,
                    replay.ToOccurredAtUtc,
                    replay.IsDryRun,
                    replay.ResetState,
                    replay.BatchSize,
                    replay.MaxEvents,
                    replay.ThrottleMilliseconds,
                    replay.TargetedEventCount,
                    replay.ProcessedEventCount,
                    replay.BatchesProcessed,
                    replay.WasCappedByMaxEvents,
                    replay.ReplayedCount,
                    replay.AppliedCount,
                    replay.FailedCount,
                    replay.EnvelopeProjectionRowCount,
                    replay.TransactionProjectionRowCount,
                    replay.StartedAtUtc,
                    replay.CompletedAtUtc,
                    replay.Status,
                    replay.ErrorMessage));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("ReplayReportingProjections")
            .WithOpenApi();

        v1.MapGet("/reports/projections/replay-runs", async (
                Guid? familyId,
                int? take,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IReportingProjectionService reportingProjectionService,
                CancellationToken cancellationToken) =>
            {
                if (familyId.HasValue
                    && !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var runs = await reportingProjectionService.ListReplayRunsAsync(
                    familyId,
                    take ?? 20,
                    cancellationToken);

                return Results.Ok(runs.Select(run => new ReportingProjectionReplayRunResponse(
                    run.Id,
                    run.FamilyId,
                    run.ProjectionSet,
                    run.FromOccurredAtUtc,
                    run.ToOccurredAtUtc,
                    run.IsDryRun,
                    run.ResetState,
                    run.BatchSize,
                    run.MaxEvents,
                    run.ThrottleMilliseconds,
                    run.TargetedEventCount,
                    run.ProcessedEventCount,
                    run.AppliedCount,
                    run.FailedCount,
                    run.BatchesProcessed,
                    run.WasCappedByMaxEvents,
                    run.Status,
                    run.RequestedByUserId,
                    run.ErrorMessage,
                    run.StartedAtUtc,
                    run.CompletedAtUtc)).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("ListReportingProjectionReplayRuns")
            .WithOpenApi();

        v1.MapGet("/reports/projections/replay-runs/{replayRunId:guid}", async (
                Guid replayRunId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IReportingProjectionService reportingProjectionService,
                CancellationToken cancellationToken) =>
            {
                var run = await reportingProjectionService.GetReplayRunAsync(replayRunId, cancellationToken);
                if (run is null)
                {
                    return Results.NotFound();
                }

                if (run.FamilyId.HasValue
                    && !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, run.FamilyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                return Results.Ok(new ReportingProjectionReplayRunResponse(
                    run.Id,
                    run.FamilyId,
                    run.ProjectionSet,
                    run.FromOccurredAtUtc,
                    run.ToOccurredAtUtc,
                    run.IsDryRun,
                    run.ResetState,
                    run.BatchSize,
                    run.MaxEvents,
                    run.ThrottleMilliseconds,
                    run.TargetedEventCount,
                    run.ProcessedEventCount,
                    run.AppliedCount,
                    run.FailedCount,
                    run.BatchesProcessed,
                    run.WasCappedByMaxEvents,
                    run.Status,
                    run.RequestedByUserId,
                    run.ErrorMessage,
                    run.StartedAtUtc,
                    run.CompletedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("GetReportingProjectionReplayRun")
            .WithOpenApi();
    }
}
