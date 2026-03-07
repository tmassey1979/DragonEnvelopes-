using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Imports;
using DragonEnvelopes.Infrastructure.Persistence;

namespace DragonEnvelopes.Api.Endpoints;

internal static partial class AccountAndTransactionEndpoints
{
    private static void MapTransactionImportEndpoints(RouteGroupBuilder v1)
    {
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
    }
}
