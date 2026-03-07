using System.Security.Claims;
using DragonEnvelopes.Application.Cqrs;
using DragonEnvelopes.Application.Cqrs.Planning;
using DragonEnvelopes.Contracts.Budgets;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Ledger.Api.CrossCutting.Auth;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Ledger.Api.Endpoints;

internal static partial class PlanningEndpoints
{
    private static void MapBudgetPlanningEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPost("/budgets", async (
                CreateBudgetRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ICommandBus commandBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var budget = await commandBus.SendAsync(
                    new CreateBudgetCommand(
                        request.FamilyId,
                        request.Month,
                        request.TotalIncome),
                    cancellationToken);
                return Results.Created($"/api/v1/budgets/{budget.Id}", EndpointMappers.MapBudgetResponse(budget));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreateBudget")
            .WithOpenApi();

        v1.MapGet("/budgets/{familyId:guid}/{month}", async (
                Guid familyId,
                string month,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IQueryBus queryBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var budget = await queryBus.QueryAsync(
                    new GetBudgetByMonthQuery(familyId, month),
                    cancellationToken);
                return budget is null
                    ? Results.NotFound()
                    : Results.Ok(EndpointMappers.MapBudgetResponse(budget));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetBudgetByMonth")
            .WithOpenApi();

        v1.MapPut("/budgets/{budgetId:guid}", async (
                Guid budgetId,
                UpdateBudgetRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ICommandBus commandBus,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.Budgets
                    .AsNoTracking()
                    .Where(x => x.Id == budgetId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var budget = await commandBus.SendAsync(
                    new UpdateBudgetCommand(
                        budgetId,
                        request.TotalIncome),
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapBudgetResponse(budget));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpdateBudget")
            .WithOpenApi();

        v1.MapGet("/budgets/rollover/preview", async (
                Guid familyId,
                string month,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IQueryBus queryBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var preview = await queryBus.QueryAsync(
                    new PreviewEnvelopeRolloverQuery(familyId, month),
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapEnvelopeRolloverPreviewResponse(preview));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("PreviewEnvelopeRollover")
            .WithOpenApi();

        v1.MapPost("/budgets/rollover/apply", async (
                ApplyEnvelopeRolloverRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ICommandBus commandBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var applied = await commandBus.SendAsync(
                    new ApplyEnvelopeRolloverCommand(
                        request.FamilyId,
                        request.Month,
                        user.FindFirstValue("sub")),
                    cancellationToken);

                return Results.Ok(EndpointMappers.MapEnvelopeRolloverApplyResponse(applied));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("ApplyEnvelopeRollover")
            .WithOpenApi();
    }
}
