using System.Security.Claims;
using DragonEnvelopes.Application.Cqrs;
using DragonEnvelopes.Application.Cqrs.Imports;
using DragonEnvelopes.Contracts.Imports;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Ledger.Api.CrossCutting.Auth;

namespace DragonEnvelopes.Ledger.Api.Endpoints;

internal static partial class AccountAndTransactionEndpoints
{
    private static void MapTransactionImportEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPost("/imports/transactions/preview", async (
                ImportPreviewRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IQueryBus queryBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var preview = await queryBus.QueryAsync(
                    new PreviewTransactionImportQuery(
                        request.FamilyId,
                        request.AccountId,
                        request.CsvContent,
                        request.Delimiter,
                        request.HeaderMappings),
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
                ICommandBus commandBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var result = await commandBus.SendAsync(
                    new CommitTransactionImportCommand(
                        request.FamilyId,
                        request.AccountId,
                        request.CsvContent,
                        request.Delimiter,
                        request.HeaderMappings,
                        request.AcceptedRowNumbers),
                    cancellationToken);
                return Results.Ok(EndpointMappers.MapImportCommitResponse(result));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CommitTransactionImport")
            .WithOpenApi();
    }
}
