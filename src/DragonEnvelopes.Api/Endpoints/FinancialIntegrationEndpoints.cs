using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Financial;
using DragonEnvelopes.Infrastructure.Persistence;

namespace DragonEnvelopes.Api.Endpoints;

internal static class FinancialIntegrationEndpoints
{
    public static RouteGroupBuilder MapFinancialIntegrationEndpoints(this RouteGroupBuilder v1)
    {
        v1.MapGet("/families/{familyId:guid}/financial/status", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFinancialIntegrationService financialIntegrationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var status = await financialIntegrationService.GetStatusAsync(familyId, cancellationToken);
                return Results.Ok(new FamilyFinancialStatusResponse(
                    status.FamilyId,
                    status.PlaidConnected,
                    status.PlaidItemId,
                    status.StripeConnected,
                    status.StripeCustomerId,
                    status.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetFamilyFinancialStatus")
            .WithOpenApi();

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
                    status.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ExchangePlaidPublicToken")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/financial/stripe/setup-intent", async (
                Guid familyId,
                CreateStripeSetupIntentRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFinancialIntegrationService financialIntegrationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var setupIntent = await financialIntegrationService.CreateStripeSetupIntentAsync(
                    familyId,
                    request.Email,
                    request.Name,
                    cancellationToken);

                return Results.Ok(new CreateStripeSetupIntentResponse(
                    setupIntent.CustomerId,
                    setupIntent.SetupIntentId,
                    setupIntent.ClientSecret));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreateStripeSetupIntent")
            .WithOpenApi();

        return v1;
    }
}
