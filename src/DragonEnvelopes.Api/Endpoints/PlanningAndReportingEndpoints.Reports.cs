using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Reports;
using DragonEnvelopes.Infrastructure.Persistence;

namespace DragonEnvelopes.Api.Endpoints;

internal static partial class PlanningAndReportingEndpoints
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
                int? batchSize,
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
                    familyId,
                    batchSize ?? 500,
                    cancellationToken);
                return Results.Ok(new ReportingProjectionReplayResponse(
                    replay.FamilyId,
                    replay.ReplayedCount,
                    replay.AppliedCount,
                    replay.FailedCount,
                    replay.EnvelopeProjectionRowCount,
                    replay.TransactionProjectionRowCount,
                    replay.StartedAtUtc,
                    replay.CompletedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("ReplayReportingProjections")
            .WithOpenApi();
    }
}
