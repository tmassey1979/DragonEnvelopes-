using System.Security.Claims;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Scenarios;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Ledger.Api.CrossCutting.Auth;

namespace DragonEnvelopes.Ledger.Api.Endpoints;

internal static partial class PlanningEndpoints
{
    private static void MapScenarioPlanningEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPost("/scenarios/simulate", async (
                SimulateScenarioRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IAccountService accountService,
                IScenarioSimulationService scenarioSimulationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var accounts = await accountService.ListAsync(request.FamilyId, cancellationToken);
                var startingBalance = accounts.Sum(static account => account.Balance);
                var simulation = await scenarioSimulationService.SimulateAsync(
                    request.FamilyId,
                    request.MonthlyIncome,
                    request.FixedExpenses,
                    request.DiscretionaryCutPercent,
                    request.MonthHorizon,
                    startingBalance,
                    cancellationToken);

                return Results.Ok(EndpointMappers.MapScenarioSimulationResponse(simulation));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("SimulateScenario")
            .WithOpenApi();
    }
}
