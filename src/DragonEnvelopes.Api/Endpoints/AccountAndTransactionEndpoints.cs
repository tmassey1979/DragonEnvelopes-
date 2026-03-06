using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Accounts;
using DragonEnvelopes.Contracts.Imports;
using DragonEnvelopes.Contracts.Transactions;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Api.Endpoints;

internal static class AccountAndTransactionEndpoints
{
    public static RouteGroupBuilder MapAccountAndTransactionEndpoints(this RouteGroupBuilder v1)
    {
        v1.MapPost("/accounts", async (
                CreateAccountRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IAccountService accountService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var account = await accountService.CreateAsync(
                    request.FamilyId,
                    request.Name,
                    request.Type,
                    request.OpeningBalance,
                    cancellationToken);

                return Results.Created($"/api/v1/accounts/{account.Id}", EndpointMappers.MapAccountResponse(account));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreateAccount")
            .WithOpenApi();

        v1.MapGet("/accounts", async (
                Guid? familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IAccountService accountService,
                CancellationToken cancellationToken) =>
            {
                if (!familyId.HasValue)
                {
                    return Results.BadRequest("familyId is required.");
                }

                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId.Value, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var accounts = await accountService.ListAsync(familyId, cancellationToken);
                return Results.Ok(accounts.Select(EndpointMappers.MapAccountResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListAccounts")
            .WithOpenApi();

        v1.MapPost("/transactions", async (
                CreateTransactionRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ITransactionService transactionService,
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

                var transaction = await transactionService.CreateAsync(
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
                        .ToArray(),
                    cancellationToken);
                return Results.Created($"/api/v1/transactions/{transaction.Id}", EndpointMappers.MapTransactionResponse(transaction));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreateTransaction")
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

        v1.MapGet("/transactions", async (
                Guid? accountId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ITransactionService transactionService,
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

                var transactions = await transactionService.ListAsync(accountId, cancellationToken);
                return Results.Ok(transactions.Select(EndpointMappers.MapTransactionResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListTransactions")
            .WithOpenApi();

        v1.MapPost("/imports/transactions/preview", async (
                ImportPreviewRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IImportService importService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var preview = await importService.PreviewTransactionsAsync(
                    request.FamilyId,
                    request.AccountId,
                    request.CsvContent,
                    request.Delimiter,
                    request.HeaderMappings,
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapImportPreviewResponse(preview));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("PreviewTransactionImport")
            .WithOpenApi();

        v1.MapPost("/imports/transactions/commit", async (
                ImportCommitRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IImportService importService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var result = await importService.CommitTransactionsAsync(
                    request.FamilyId,
                    request.AccountId,
                    request.CsvContent,
                    request.Delimiter,
                    request.HeaderMappings,
                    request.AcceptedRowNumbers,
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapImportCommitResponse(result));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CommitTransactionImport")
            .WithOpenApi();

        return v1;
    }
}
