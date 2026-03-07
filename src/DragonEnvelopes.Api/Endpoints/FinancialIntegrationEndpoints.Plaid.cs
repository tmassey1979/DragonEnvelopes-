using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Financial;
using DragonEnvelopes.Infrastructure.Persistence;

namespace DragonEnvelopes.Api.Endpoints;

internal static partial class FinancialIntegrationEndpoints
{
    private static void MapPlaidEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPost("/families/{familyId:guid}/financial/plaid/link-token", async (
                Guid familyId,
                CreatePlaidLinkTokenRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFinancialIntegrationService financialIntegrationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var clientUserId = user.FindFirstValue("sub") ?? request.ClientUserId;
                var token = await financialIntegrationService.CreatePlaidLinkTokenAsync(
                    familyId,
                    clientUserId,
                    request.ClientName,
                    cancellationToken);

                return Results.Ok(new CreatePlaidLinkTokenResponse(token.LinkToken, token.ExpiresAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreatePlaidLinkToken")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/financial/plaid/exchange-public-token", async (
                Guid familyId,
                ExchangePlaidPublicTokenRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFinancialIntegrationService financialIntegrationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var status = await financialIntegrationService.ExchangePlaidPublicTokenAsync(
                    familyId,
                    request.PublicToken,
                    cancellationToken);

                return Results.Ok(new FamilyFinancialStatusResponse(
                    status.FamilyId,
                    status.PlaidConnected,
                    status.PlaidItemId,
                    status.StripeConnected,
                    status.StripeCustomerId,
                    status.UpdatedAtUtc,
                    status.ReconciliationDriftThreshold));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ExchangePlaidPublicToken")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/financial/plaid/account-links", async (
                Guid familyId,
                CreatePlaidAccountLinkRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IPlaidTransactionSyncService plaidTransactionSyncService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var link = await plaidTransactionSyncService.UpsertAccountLinkAsync(
                    familyId,
                    request.AccountId,
                    request.PlaidAccountId,
                    cancellationToken);
                return Results.Ok(new PlaidAccountLinkResponse(
                    link.Id,
                    link.FamilyId,
                    link.AccountId,
                    link.PlaidAccountId,
                    link.CreatedAtUtc,
                    link.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpsertPlaidAccountLink")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/financial/plaid/account-links", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IPlaidTransactionSyncService plaidTransactionSyncService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var links = await plaidTransactionSyncService.ListAccountLinksAsync(familyId, cancellationToken);
                return Results.Ok(links.Select(link => new PlaidAccountLinkResponse(
                    link.Id,
                    link.FamilyId,
                    link.AccountId,
                    link.PlaidAccountId,
                    link.CreatedAtUtc,
                    link.UpdatedAtUtc)).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListPlaidAccountLinks")
            .WithOpenApi();

        v1.MapDelete("/families/{familyId:guid}/financial/plaid/account-links/{linkId:guid}", async (
                Guid familyId,
                Guid linkId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IPlaidTransactionSyncService plaidTransactionSyncService,
                ILogger<Program> logger,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                await plaidTransactionSyncService.DeleteAccountLinkAsync(
                    familyId,
                    linkId,
                    cancellationToken);

                logger.LogInformation(
                    "Plaid account link removed. FamilyId={FamilyId}, LinkId={LinkId}, UserId={UserId}",
                    familyId,
                    linkId,
                    user.FindFirstValue("sub") ?? "unknown");

                return Results.NoContent();
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("DeletePlaidAccountLink")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/financial/plaid/sync-transactions", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IPlaidTransactionSyncService plaidTransactionSyncService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var sync = await plaidTransactionSyncService.SyncFamilyAsync(familyId, cancellationToken);
                return Results.Ok(new PlaidTransactionSyncResponse(
                    sync.FamilyId,
                    sync.PulledCount,
                    sync.InsertedCount,
                    sync.DedupedCount,
                    sync.UnmappedCount,
                    sync.NextCursor,
                    sync.ProcessedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("SyncPlaidTransactions")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/financial/plaid/refresh-balances", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IPlaidBalanceReconciliationService plaidBalanceReconciliationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var refresh = await plaidBalanceReconciliationService.RefreshFamilyBalancesAsync(familyId, cancellationToken);
                return Results.Ok(new PlaidBalanceRefreshResponse(
                    refresh.FamilyId,
                    refresh.RefreshedCount,
                    refresh.DriftedCount,
                    refresh.TotalAbsoluteDrift,
                    refresh.RefreshedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("RefreshPlaidBalances")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/financial/plaid/reconciliation", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IPlaidBalanceReconciliationService plaidBalanceReconciliationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var report = await plaidBalanceReconciliationService.GetReconciliationReportAsync(familyId, cancellationToken);
                return Results.Ok(new PlaidReconciliationReportResponse(
                    report.FamilyId,
                    report.GeneratedAtUtc,
                    report.Accounts.Select(account => new PlaidReconciliationAccountResponse(
                        account.AccountId,
                        account.AccountName,
                        account.PlaidAccountId,
                        account.InternalBalance,
                        account.ProviderBalance,
                        account.DriftAmount,
                        account.IsDrifted)).ToArray()));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetPlaidReconciliationReport")
            .WithOpenApi();
    }
}
