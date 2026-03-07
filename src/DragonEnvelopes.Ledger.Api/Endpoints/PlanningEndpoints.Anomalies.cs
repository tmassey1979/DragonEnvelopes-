using System.Security.Claims;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Ledger.Api.CrossCutting.Auth;

namespace DragonEnvelopes.Ledger.Api.Endpoints;

internal static partial class PlanningEndpoints
{
    private static void MapSpendAnomalyPlanningEndpoints(RouteGroupBuilder v1)
    {
        v1.MapGet("/spend-anomalies", async (
                Guid familyId,
                int? take,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ISpendAnomalyService spendAnomalyService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var events = await spendAnomalyService.ListByFamilyAsync(
                    familyId,
                    take ?? 50,
                    cancellationToken);
                return Results.Ok(events.Select(EndpointMappers.MapSpendAnomalyEventResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListSpendAnomalyEvents")
            .WithOpenApi();
    }
}
