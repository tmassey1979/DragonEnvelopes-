using System.Security.Claims;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Ledger.Api.CrossCutting.Auth;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Ledger.Api.Endpoints;

internal static partial class AutomationEndpoints
{
    private static void MapAutomationRuleStateEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPost("/automation/rules/{ruleId:guid}/enable", async (
                Guid ruleId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IAutomationRuleService automationRuleService,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.AutomationRules
                    .AsNoTracking()
                    .Where(x => x.Id == ruleId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                await automationRuleService.EnableAsync(ruleId, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("EnableAutomationRule")
            .WithOpenApi();

        v1.MapPost("/automation/rules/{ruleId:guid}/disable", async (
                Guid ruleId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IAutomationRuleService automationRuleService,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.AutomationRules
                    .AsNoTracking()
                    .Where(x => x.Id == ruleId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                await automationRuleService.DisableAsync(ruleId, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("DisableAutomationRule")
            .WithOpenApi();
    }
}
