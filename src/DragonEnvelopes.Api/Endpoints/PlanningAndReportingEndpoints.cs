using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Api.Services;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Budgets;
using DragonEnvelopes.Contracts.Envelopes;
using DragonEnvelopes.Contracts.RecurringBills;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Api.Endpoints;

internal static class PlanningAndReportingEndpoints
{
    public static RouteGroupBuilder MapPlanningAndReportingEndpoints(this RouteGroupBuilder v1)
    {
        v1.MapPost("/envelopes", async (
                CreateEnvelopeRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopeService envelopeService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var envelope = await envelopeService.CreateAsync(
                    request.FamilyId,
                    request.Name,
                    request.MonthlyBudget,
                    cancellationToken);
                return Results.Created($"/api/v1/envelopes/{envelope.Id}", EndpointMappers.MapEnvelopeResponse(envelope));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreateEnvelope")
            .WithOpenApi();

        v1.MapGet("/envelopes/{envelopeId:guid}", async (
                Guid envelopeId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopeService envelopeService,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.Envelopes
                    .AsNoTracking()
                    .Where(x => x.Id == envelopeId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var envelope = await envelopeService.GetByIdAsync(envelopeId, cancellationToken);
                return envelope is null
                    ? Results.NotFound()
                    : Results.Ok(EndpointMappers.MapEnvelopeResponse(envelope));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetEnvelopeById")
            .WithOpenApi();

        v1.MapGet("/envelopes", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopeService envelopeService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var envelopes = await envelopeService.ListByFamilyAsync(familyId, cancellationToken);
                return Results.Ok(envelopes.Select(EndpointMappers.MapEnvelopeResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListEnvelopes")
            .WithOpenApi();

        v1.MapPut("/envelopes/{envelopeId:guid}", async (
                Guid envelopeId,
                UpdateEnvelopeRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopeService envelopeService,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.Envelopes
                    .AsNoTracking()
                    .Where(x => x.Id == envelopeId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var envelope = await envelopeService.UpdateAsync(
                    envelopeId,
                    request.Name,
                    request.MonthlyBudget,
                    request.IsArchived,
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapEnvelopeResponse(envelope));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpdateEnvelope")
            .WithOpenApi();

        v1.MapPost("/envelopes/{envelopeId:guid}/archive", async (
                Guid envelopeId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopeService envelopeService,
                CancellationToken cancellationToken) =>
            {
                var familyId = await dbContext.Envelopes
                    .AsNoTracking()
                    .Where(x => x.Id == envelopeId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!familyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var envelope = await envelopeService.ArchiveAsync(envelopeId, cancellationToken);
                return Results.Ok(EndpointMappers.MapEnvelopeResponse(envelope));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ArchiveEnvelope")
            .WithOpenApi();

        v1.MapPost("/budgets", async (
                CreateBudgetRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IBudgetService budgetService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var budget = await budgetService.CreateAsync(
                    request.FamilyId,
                    request.Month,
                    request.TotalIncome,
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
                IBudgetService budgetService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var budget = await budgetService.GetByMonthAsync(familyId, month, cancellationToken);
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
                IBudgetService budgetService,
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

                var budget = await budgetService.UpdateAsync(
                    budgetId,
                    request.TotalIncome,
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapBudgetResponse(budget));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpdateBudget")
            .WithOpenApi();

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

        return v1;
    }
}
