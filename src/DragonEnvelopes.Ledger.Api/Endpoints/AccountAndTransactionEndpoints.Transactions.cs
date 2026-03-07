using System.Security.Claims;
using DragonEnvelopes.Application.Cqrs;
using DragonEnvelopes.Application.Cqrs.Transactions;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Transactions;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Ledger.Api.CrossCutting.Auth;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Ledger.Api.Endpoints;

internal static partial class AccountAndTransactionEndpoints
{
    private static void MapTransactionEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPost("/transactions", async (
                CreateTransactionRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ICommandBus commandBus,
                IApprovalWorkflowService approvalWorkflowService,
                CancellationToken cancellationToken) =>
            {
                var accountFamilyId = await dbContext.Accounts
                    .AsNoTracking()
                    .Where(x => x.Id == request.AccountId)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!accountFamilyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, accountFamilyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var userId = user.FindFirstValue("sub");
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Results.Forbid();
                }

                var memberRole = await dbContext.FamilyMembers
                    .AsNoTracking()
                    .Where(member => member.FamilyId == accountFamilyId.Value && member.KeycloakUserId == userId)
                    .Select(member => (string?)member.Role.ToString())
                    .FirstOrDefaultAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(memberRole))
                {
                    return Results.Forbid();
                }

                var blockedRequest = await approvalWorkflowService.TryCreateBlockedRequestAsync(
                    accountFamilyId.Value,
                    request.AccountId,
                    userId,
                    memberRole,
                    request.Amount,
                    request.Description,
                    request.Merchant,
                    request.OccurredAt,
                    request.Category,
                    request.EnvelopeId,
                    cancellationToken);
                if (blockedRequest is not null)
                {
                    return Results.Accepted(
                        $"/api/v1/approvals/requests/{blockedRequest.Id}",
                        EndpointMappers.MapApprovalRequestResponse(blockedRequest));
                }

                var command = new CreateTransactionCommand(
                    request.AccountId,
                    request.Amount,
                    request.Description,
                    request.Merchant,
                    request.OccurredAt,
                    request.Category,
                    request.EnvelopeId,
                    request.Splits is { Count: > 0 },
                    request.Splits?
                        .Select(static split => new TransactionSplitCreateDetails(
                            split.EnvelopeId,
                            split.Amount,
                            split.Category,
                            split.Notes))
                        .ToArray());
                var transaction = await commandBus.SendAsync(command, cancellationToken);
                return Results.Created($"/api/v1/transactions/{transaction.Id}", EndpointMappers.MapTransactionResponse(transaction));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreateTransaction")
            .WithOpenApi();

        v1.MapPost("/transactions/envelope-transfers", async (
                CreateEnvelopeTransferRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopeTransferService envelopeTransferService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var transfer = await envelopeTransferService.CreateAsync(
                    request.FamilyId,
                    request.AccountId,
                    request.FromEnvelopeId,
                    request.ToEnvelopeId,
                    request.Amount,
                    request.OccurredAt,
                    request.Notes,
                    cancellationToken);

                return Results.Created(
                    $"/api/v1/transactions/envelope-transfers/{transfer.TransferId}",
                    EndpointMappers.MapEnvelopeTransferResponse(transfer));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("CreateEnvelopeTransfer")
            .WithOpenApi();

        v1.MapPut("/transactions/{transactionId:guid}", async (
                Guid transactionId,
                UpdateTransactionRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ITransactionService transactionService,
                CancellationToken cancellationToken) =>
            {
                var transactionFamilyId = await dbContext.Transactions
                    .AsNoTracking()
                    .Where(x => x.Id == transactionId)
                    .Join(
                        dbContext.Accounts.AsNoTracking(),
                        transaction => transaction.AccountId,
                        account => account.Id,
                        (_, account) => (Guid?)account.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!transactionFamilyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, transactionFamilyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var transaction = await transactionService.UpdateAsync(
                    transactionId,
                    request.Description,
                    request.Merchant,
                    request.Category,
                    request.ReplaceAllocation,
                    request.EnvelopeId,
                    request.Splits?
                        .Select(static split => new TransactionSplitCreateDetails(
                            split.EnvelopeId,
                            split.Amount,
                            split.Category,
                            split.Notes))
                        .ToArray(),
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapTransactionResponse(transaction));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpdateTransaction")
            .WithOpenApi();

        v1.MapDelete("/transactions/{transactionId:guid}", async (
                Guid transactionId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ITransactionService transactionService,
                CancellationToken cancellationToken) =>
            {
                var transactionFamilyId = await dbContext.Transactions
                    .AsNoTracking()
                    .Where(x => x.Id == transactionId)
                    .Join(
                        dbContext.Accounts.AsNoTracking(),
                        transaction => transaction.AccountId,
                        account => account.Id,
                        (_, account) => (Guid?)account.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!transactionFamilyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, transactionFamilyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                await transactionService.DeleteAsync(transactionId, user.FindFirstValue("sub"), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("DeleteTransaction")
            .WithOpenApi();

        v1.MapPost("/transactions/{transactionId:guid}/restore", async (
                Guid transactionId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ITransactionService transactionService,
                CancellationToken cancellationToken) =>
            {
                var transactionFamilyId = await dbContext.Transactions
                    .AsNoTracking()
                    .Where(x => x.Id == transactionId)
                    .Join(
                        dbContext.Accounts.AsNoTracking(),
                        transaction => transaction.AccountId,
                        account => account.Id,
                        (_, account) => (Guid?)account.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!transactionFamilyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, transactionFamilyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var transaction = await transactionService.RestoreAsync(transactionId, cancellationToken);
                return Results.Ok(EndpointMappers.MapTransactionResponse(transaction));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("RestoreTransaction")
            .WithOpenApi();

        v1.MapGet("/transactions", async (
                Guid? accountId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IQueryBus queryBus,
                CancellationToken cancellationToken) =>
            {
                if (!accountId.HasValue)
                {
                    return Results.BadRequest("accountId is required.");
                }

                var accountFamilyId = await dbContext.Accounts
                    .AsNoTracking()
                    .Where(x => x.Id == accountId.Value)
                    .Select(x => (Guid?)x.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!accountFamilyId.HasValue || !await EndpointAccessGuards.UserHasFamilyAccessAsync(user, accountFamilyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var transactions = await queryBus.QueryAsync(new ListTransactionsByAccountQuery(accountId), cancellationToken);
                return Results.Ok(transactions.Select(EndpointMappers.MapTransactionResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListTransactions")
            .WithOpenApi();

        v1.MapGet("/transactions/deleted", async (
                Guid familyId,
                int? days,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ITransactionService transactionService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var transactions = await transactionService.ListDeletedAsync(familyId, days ?? 30, cancellationToken);
                return Results.Ok(transactions.Select(EndpointMappers.MapTransactionResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListDeletedTransactions")
            .WithOpenApi();

    }
}



