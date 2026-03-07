using System.Security.Claims;
using DragonEnvelopes.Application.Cqrs;
using DragonEnvelopes.Application.Cqrs.Accounts;
using DragonEnvelopes.Contracts.Accounts;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Ledger.Api.CrossCutting.Auth;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Ledger.Api.Endpoints;

internal static partial class AccountAndTransactionEndpoints
{
    private static void MapAccountEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPost("/accounts", async (
                CreateAccountRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ICommandBus commandBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, request.FamilyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var account = await commandBus.SendAsync(
                    new CreateAccountCommand(
                        request.FamilyId,
                        request.Name,
                        request.Type,
                        request.OpeningBalance),
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
                IQueryBus queryBus,
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

                var accounts = await queryBus.QueryAsync(
                    new ListAccountsQuery(familyId),
                    cancellationToken);
                return Results.Ok(accounts.Select(EndpointMappers.MapAccountResponse).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListAccounts")
            .WithOpenApi();

    }
}
