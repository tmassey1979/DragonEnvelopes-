using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Automation;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Api.Endpoints;

internal static class AutomationEndpoints
{
    public static RouteGroupBuilder MapAutomationEndpoints(this RouteGroupBuilder v1)
    {
        v1.MapPost("/automation/rules", async (
                CreateAutomationRuleRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IAutomationRuleService automationRuleService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var rule = await automationRuleService.CreateAsync(
                    request.FamilyId,
                    request.Name,
                    request.RuleType,
                    request.Priority,
                    request.IsEnabled,
                    request.ConditionsJson,
                    request.ActionJson,
                    cancellationToken);

                return Results.Created($"/api/v1/automation/rules/{rule.Id}", EndpointMappers.MapAutomationRuleResponse(rule));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreateAutomationRule")
            .WithOpenApi();

        v1.MapGet("/automation/rules", async (
                Guid familyId,
                string? type,
                bool? enabled,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IAutomationRuleService automationRuleService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var rules = await automationRuleService.ListAsync(familyId, type, enabled, cancellationToken);
                return Results.Ok(rules.Select(EndpointMappers.MapAutomationRuleResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListAutomationRules")
            .WithOpenApi();

        v1.MapGet("/automation/rules/{ruleId:guid}", async (
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

                var rule = await automationRuleService.GetByIdAsync(ruleId, cancellationToken);
                return rule is null
                    ? Results.NotFound()
                    : Results.Ok(EndpointMappers.MapAutomationRuleResponse(rule));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetAutomationRuleById")
            .WithOpenApi();

        v1.MapPut("/automation/rules/{ruleId:guid}", async (
                Guid ruleId,
                UpdateAutomationRuleRequest request,
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

                var rule = await automationRuleService.UpdateAsync(
                    ruleId,
                    request.Name,
                    request.Priority,
                    request.IsEnabled,
                    request.ConditionsJson,
                    request.ActionJson,
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapAutomationRuleResponse(rule));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpdateAutomationRule")
            .WithOpenApi();

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

        v1.MapDelete("/automation/rules/{ruleId:guid}", async (
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

                await automationRuleService.DeleteAsync(ruleId, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("DeleteAutomationRule")
            .WithOpenApi();

        return v1;
    }
}
