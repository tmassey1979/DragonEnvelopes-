using System.Security.Claims;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.RecurringBills;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Ledger.Api.CrossCutting.Auth;
using DragonEnvelopes.Ledger.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Ledger.Api.Endpoints;

internal static partial class PlanningEndpoints
{
    private static void MapRecurringBillPlanningEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPost("/recurring-bills", async (
                CreateRecurringBillRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IRecurringBillService recurringBillService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var recurringBill = await recurringBillService.CreateAsync(
                    request.FamilyId,
                    request.Name,
                    request.Merchant,
                    request.Amount,
                    request.Frequency,
                    request.DayOfMonth,
                    request.StartDate,
                    request.EndDate,
                    request.IsActive,
                    cancellationToken);
                return Results.Created($"/api/v1/recurring-bills/{recurringBill.Id}", EndpointMappers.MapRecurringBillResponse(recurringBill));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreateRecurringBill")
            .WithOpenApi();

        v1.MapGet("/recurring-bills", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IRecurringBillService recurringBillService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var recurringBills = await recurringBillService.ListByFamilyAsync(familyId, cancellationToken);
                return Results.Ok(recurringBills.Select(EndpointMappers.MapRecurringBillResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListRecurringBills")
            .WithOpenApi();

        v1.MapPut("/recurring-bills/{recurringBillId:guid}", async (
                Guid recurringBillId,
                UpdateRecurringBillRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IRecurringBillService recurringBillService,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.RecurringBills
                    .AsNoTracking()
                    .Where(x => x.Id == recurringBillId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var recurringBill = await recurringBillService.UpdateAsync(
                    recurringBillId,
                    request.Name,
                    request.Merchant,
                    request.Amount,
                    request.Frequency,
                    request.DayOfMonth,
                    request.StartDate,
                    request.EndDate,
                    request.IsActive,
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapRecurringBillResponse(recurringBill));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpdateRecurringBill")
            .WithOpenApi();

        v1.MapDelete("/recurring-bills/{recurringBillId:guid}", async (
                Guid recurringBillId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IRecurringBillService recurringBillService,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.RecurringBills
                    .AsNoTracking()
                    .Where(x => x.Id == recurringBillId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                await recurringBillService.DeleteAsync(recurringBillId, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("DeleteRecurringBill")
            .WithOpenApi();

        v1.MapGet("/recurring-bills/projection", async (
                Guid familyId,
                DateOnly from,
                DateOnly to,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IRecurringBillService recurringBillService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var projection = await recurringBillService.ProjectAsync(familyId, from, to, cancellationToken);
                return Results.Ok(projection.Select(EndpointMappers.MapRecurringBillProjectionItemResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ProjectRecurringBills")
            .WithOpenApi();

        v1.MapGet("/recurring-bills/{recurringBillId:guid}/executions", async (
                Guid recurringBillId,
                int? take,
                string? result,
                DateOnly? fromDate,
                DateOnly? toDate,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IRecurringBillService recurringBillService,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.RecurringBills
                    .AsNoTracking()
                    .Where(x => x.Id == recurringBillId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var executions = await recurringBillService.ListExecutionsAsync(
                    recurringBillId,
                    take ?? 25,
                    result,
                    fromDate,
                    toDate,
                    cancellationToken);

                return Results.Ok(executions.Select(EndpointMappers.MapRecurringBillExecutionResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListRecurringBillExecutions")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/recurring-bills/auto-post/run", async (
                Guid familyId,
                DateOnly? dueDate,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IRecurringAutoPostService recurringAutoPostService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var summary = await recurringAutoPostService.RunAsync(
                    familyId,
                    dueDate,
                    cancellationToken);

                var response = new RecurringAutoPostRunResponse(
                    familyId,
                    summary.DueDate,
                    summary.DueBillCount,
                    summary.PostedCount,
                    summary.SkippedCount,
                    summary.FailedCount,
                    summary.AlreadyProcessedCount,
                    summary.Executions
                        .Select(static execution => new RecurringAutoPostExecutionResponse(
                            execution.RecurringBillId,
                            execution.RecurringBillName,
                            execution.Result,
                            execution.TransactionId,
                            execution.Notes))
                        .ToArray());

                return Results.Ok(response);
            })
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("RunRecurringAutoPost")
            .WithOpenApi();
    }
}
