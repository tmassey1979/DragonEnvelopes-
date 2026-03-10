using System.Security.Claims;
using DragonEnvelopes.Application.Cqrs;
using DragonEnvelopes.Application.Cqrs.Financial;
using DragonEnvelopes.Financial.Api.CrossCutting.Auth;
using DragonEnvelopes.Contracts.Financial;
using DragonEnvelopes.Infrastructure.Persistence;

namespace DragonEnvelopes.Financial.Api.Endpoints;

internal static partial class FinancialIntegrationEndpoints
{
    private static void MapStripeAccountEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPost("/families/{familyId:guid}/financial/stripe/setup-intent", async (
                Guid familyId,
                CreateStripeSetupIntentRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ICommandBus commandBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var setupIntent = await commandBus.SendAsync(
                    new CreateStripeSetupIntentCommand(
                        familyId,
                        request.Email,
                        request.Name),
                    cancellationToken);

                return Results.Ok(new CreateStripeSetupIntentResponse(
                    setupIntent.CustomerId,
                    setupIntent.SetupIntentId,
                    setupIntent.ClientSecret));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreateStripeSetupIntent")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/financial-accounts/stripe", async (
                Guid familyId,
                Guid envelopeId,
                CreateStripeEnvelopeFinancialAccountRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ICommandBus commandBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var account = await commandBus.SendAsync(
                    new LinkStripeEnvelopeFinancialAccountCommand(
                        familyId,
                        envelopeId,
                        request.DisplayName),
                    cancellationToken);

                return Results.Ok(new EnvelopeFinancialAccountResponse(
                    account.Id,
                    account.FamilyId,
                    account.EnvelopeId,
                    account.Provider,
                    account.ProviderFinancialAccountId,
                    account.CreatedAtUtc,
                    account.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("LinkStripeEnvelopeFinancialAccount")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/envelopes/{envelopeId:guid}/financial-account", async (
                Guid familyId,
                Guid envelopeId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IQueryBus queryBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var account = await queryBus.QueryAsync(
                    new GetEnvelopeFinancialAccountQuery(
                        familyId,
                        envelopeId),
                    cancellationToken);
                return account is null
                    ? Results.NotFound()
                    : Results.Ok(new EnvelopeFinancialAccountResponse(
                        account.Id,
                        account.FamilyId,
                        account.EnvelopeId,
                        account.Provider,
                        account.ProviderFinancialAccountId,
                        account.CreatedAtUtc,
                        account.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetEnvelopeFinancialAccount")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/financial-accounts", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IQueryBus queryBus,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var accounts = await queryBus.QueryAsync(
                    new ListFamilyFinancialAccountsQuery(familyId),
                    cancellationToken);
                return Results.Ok(accounts.Select(account => new EnvelopeFinancialAccountResponse(
                    account.Id,
                    account.FamilyId,
                    account.EnvelopeId,
                    account.Provider,
                    account.ProviderFinancialAccountId,
                    account.CreatedAtUtc,
                    account.UpdatedAtUtc)).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListFamilyFinancialAccounts")
            .WithOpenApi();
    }
}

