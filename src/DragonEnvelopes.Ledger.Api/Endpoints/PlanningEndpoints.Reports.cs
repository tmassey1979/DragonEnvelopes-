using System.Security.Claims;
using DragonEnvelopes.Application.Services;
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
    }
}
