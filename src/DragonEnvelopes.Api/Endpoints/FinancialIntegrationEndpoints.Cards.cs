using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Financial;
using DragonEnvelopes.Infrastructure.Persistence;

namespace DragonEnvelopes.Api.Endpoints;

internal static partial class FinancialIntegrationEndpoints
{
    private static void MapEnvelopeCardEndpoints(RouteGroupBuilder v1)
    {
        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/virtual", async (
                Guid familyId,
                Guid envelopeId,
                CreateVirtualEnvelopeCardRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var card = await envelopePaymentCardService.IssueVirtualCardAsync(
                    familyId,
                    envelopeId,
                    request.CardholderName,
                    cancellationToken);
                return Results.Ok(new EnvelopePaymentCardResponse(
                    card.Id,
                    card.FamilyId,
                    card.EnvelopeId,
                    card.EnvelopeFinancialAccountId,
                    card.Provider,
                    card.ProviderCardId,
                    card.Type,
                    card.Status,
                    card.Brand,
                    card.Last4,
                    card.CreatedAtUtc,
                    card.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("IssueVirtualEnvelopeCard")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/physical", async (
                Guid familyId,
                Guid envelopeId,
                CreatePhysicalEnvelopeCardRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var issuance = await envelopePaymentCardService.IssuePhysicalCardAsync(
                    familyId,
                    envelopeId,
                    request.CardholderName,
                    request.RecipientName,
                    request.AddressLine1,
                    request.AddressLine2,
                    request.City,
                    request.StateOrProvince,
                    request.PostalCode,
                    request.CountryCode,
                    cancellationToken);

                return Results.Ok(new EnvelopePhysicalCardIssuanceResponse(
                    new EnvelopePaymentCardResponse(
                        issuance.Card.Id,
                        issuance.Card.FamilyId,
                        issuance.Card.EnvelopeId,
                        issuance.Card.EnvelopeFinancialAccountId,
                        issuance.Card.Provider,
                        issuance.Card.ProviderCardId,
                        issuance.Card.Type,
                        issuance.Card.Status,
                        issuance.Card.Brand,
                        issuance.Card.Last4,
                        issuance.Card.CreatedAtUtc,
                        issuance.Card.UpdatedAtUtc),
                    new EnvelopePaymentCardShipmentResponse(
                        issuance.Shipment.Id,
                        issuance.Shipment.FamilyId,
                        issuance.Shipment.EnvelopeId,
                        issuance.Shipment.CardId,
                        issuance.Shipment.RecipientName,
                        issuance.Shipment.AddressLine1,
                        issuance.Shipment.AddressLine2,
                        issuance.Shipment.City,
                        issuance.Shipment.StateOrProvince,
                        issuance.Shipment.PostalCode,
                        issuance.Shipment.CountryCode,
                        issuance.Shipment.Status,
                        issuance.Shipment.Carrier,
                        issuance.Shipment.TrackingNumber,
                        issuance.Shipment.RequestedAtUtc,
                        issuance.Shipment.UpdatedAtUtc)));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("IssuePhysicalEnvelopeCard")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards", async (
                Guid familyId,
                Guid envelopeId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var cards = await envelopePaymentCardService.ListByEnvelopeAsync(
                    familyId,
                    envelopeId,
                    cancellationToken);
                return Results.Ok(cards.Select(card => new EnvelopePaymentCardResponse(
                    card.Id,
                    card.FamilyId,
                    card.EnvelopeId,
                    card.EnvelopeFinancialAccountId,
                    card.Provider,
                    card.ProviderCardId,
                    card.Type,
                    card.Status,
                    card.Brand,
                    card.Last4,
                    card.CreatedAtUtc,
                    card.UpdatedAtUtc)).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListEnvelopeCards")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/freeze", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var card = await envelopePaymentCardService.FreezeCardAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    cancellationToken);
                return Results.Ok(new EnvelopePaymentCardResponse(
                    card.Id,
                    card.FamilyId,
                    card.EnvelopeId,
                    card.EnvelopeFinancialAccountId,
                    card.Provider,
                    card.ProviderCardId,
                    card.Type,
                    card.Status,
                    card.Brand,
                    card.Last4,
                    card.CreatedAtUtc,
                    card.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("FreezeEnvelopeCard")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/unfreeze", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var card = await envelopePaymentCardService.UnfreezeCardAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    cancellationToken);
                return Results.Ok(new EnvelopePaymentCardResponse(
                    card.Id,
                    card.FamilyId,
                    card.EnvelopeId,
                    card.EnvelopeFinancialAccountId,
                    card.Provider,
                    card.ProviderCardId,
                    card.Type,
                    card.Status,
                    card.Brand,
                    card.Last4,
                    card.CreatedAtUtc,
                    card.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UnfreezeEnvelopeCard")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/cancel", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var card = await envelopePaymentCardService.CancelCardAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    cancellationToken);
                return Results.Ok(new EnvelopePaymentCardResponse(
                    card.Id,
                    card.FamilyId,
                    card.EnvelopeId,
                    card.EnvelopeFinancialAccountId,
                    card.Provider,
                    card.ProviderCardId,
                    card.Type,
                    card.Status,
                    card.Brand,
                    card.Last4,
                    card.CreatedAtUtc,
                    card.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CancelEnvelopeCard")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/issuance", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var issuance = await envelopePaymentCardService.GetPhysicalCardIssuanceAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    cancellationToken);

                return issuance is null
                    ? Results.NotFound()
                    : Results.Ok(new EnvelopePhysicalCardIssuanceResponse(
                        new EnvelopePaymentCardResponse(
                            issuance.Card.Id,
                            issuance.Card.FamilyId,
                            issuance.Card.EnvelopeId,
                            issuance.Card.EnvelopeFinancialAccountId,
                            issuance.Card.Provider,
                            issuance.Card.ProviderCardId,
                            issuance.Card.Type,
                            issuance.Card.Status,
                            issuance.Card.Brand,
                            issuance.Card.Last4,
                            issuance.Card.CreatedAtUtc,
                            issuance.Card.UpdatedAtUtc),
                        new EnvelopePaymentCardShipmentResponse(
                            issuance.Shipment.Id,
                            issuance.Shipment.FamilyId,
                            issuance.Shipment.EnvelopeId,
                            issuance.Shipment.CardId,
                            issuance.Shipment.RecipientName,
                            issuance.Shipment.AddressLine1,
                            issuance.Shipment.AddressLine2,
                            issuance.Shipment.City,
                            issuance.Shipment.StateOrProvince,
                            issuance.Shipment.PostalCode,
                            issuance.Shipment.CountryCode,
                            issuance.Shipment.Status,
                            issuance.Shipment.Carrier,
                            issuance.Shipment.TrackingNumber,
                            issuance.Shipment.RequestedAtUtc,
                            issuance.Shipment.UpdatedAtUtc)));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetEnvelopePhysicalCardIssuance")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/issuance/refresh", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var issuance = await envelopePaymentCardService.RefreshPhysicalCardIssuanceStatusAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    cancellationToken);

                return Results.Ok(new EnvelopePhysicalCardIssuanceResponse(
                    new EnvelopePaymentCardResponse(
                        issuance.Card.Id,
                        issuance.Card.FamilyId,
                        issuance.Card.EnvelopeId,
                        issuance.Card.EnvelopeFinancialAccountId,
                        issuance.Card.Provider,
                        issuance.Card.ProviderCardId,
                        issuance.Card.Type,
                        issuance.Card.Status,
                        issuance.Card.Brand,
                        issuance.Card.Last4,
                        issuance.Card.CreatedAtUtc,
                        issuance.Card.UpdatedAtUtc),
                    new EnvelopePaymentCardShipmentResponse(
                        issuance.Shipment.Id,
                        issuance.Shipment.FamilyId,
                        issuance.Shipment.EnvelopeId,
                        issuance.Shipment.CardId,
                        issuance.Shipment.RecipientName,
                        issuance.Shipment.AddressLine1,
                        issuance.Shipment.AddressLine2,
                        issuance.Shipment.City,
                        issuance.Shipment.StateOrProvince,
                        issuance.Shipment.PostalCode,
                        issuance.Shipment.CountryCode,
                        issuance.Shipment.Status,
                        issuance.Shipment.Carrier,
                        issuance.Shipment.TrackingNumber,
                        issuance.Shipment.RequestedAtUtc,
                        issuance.Shipment.UpdatedAtUtc)));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("RefreshEnvelopePhysicalCardIssuance")
            .WithOpenApi();

        v1.MapPut("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/controls", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                UpsertEnvelopePaymentCardControlRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardControlService envelopePaymentCardControlService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var control = await envelopePaymentCardControlService.UpsertControlsAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    request.DailyLimitAmount,
                    request.AllowedMerchantCategories,
                    request.AllowedMerchantNames,
                    user.FindFirstValue("sub"),
                    cancellationToken);
                return Results.Ok(new EnvelopePaymentCardControlResponse(
                    control.Id,
                    control.FamilyId,
                    control.EnvelopeId,
                    control.CardId,
                    control.DailyLimitAmount,
                    control.AllowedMerchantCategories,
                    control.AllowedMerchantNames,
                    control.CreatedAtUtc,
                    control.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpsertEnvelopeCardControls")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/controls", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardControlService envelopePaymentCardControlService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var control = await envelopePaymentCardControlService.GetByCardAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    cancellationToken);

                return control is null
                    ? Results.NotFound()
                    : Results.Ok(new EnvelopePaymentCardControlResponse(
                        control.Id,
                        control.FamilyId,
                        control.EnvelopeId,
                        control.CardId,
                        control.DailyLimitAmount,
                        control.AllowedMerchantCategories,
                        control.AllowedMerchantNames,
                        control.CreatedAtUtc,
                        control.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetEnvelopeCardControls")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/controls/audit", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardControlService envelopePaymentCardControlService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var audit = await envelopePaymentCardControlService.ListAuditByCardAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    cancellationToken);

                return Results.Ok(audit.Select(entry => new EnvelopePaymentCardControlAuditResponse(
                    entry.Id,
                    entry.FamilyId,
                    entry.EnvelopeId,
                    entry.CardId,
                    entry.Action,
                    entry.PreviousStateJson,
                    entry.NewStateJson,
                    entry.ChangedBy,
                    entry.ChangedAtUtc)).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListEnvelopeCardControlAudit")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/controls/evaluate", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                EvaluateEnvelopeCardSpendRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardControlService envelopePaymentCardControlService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var evaluation = await envelopePaymentCardControlService.EvaluateSpendAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    request.MerchantName,
                    request.MerchantCategory,
                    request.Amount,
                    request.SpentTodayAmount,
                    cancellationToken);

                return Results.Ok(new EvaluateEnvelopeCardSpendResponse(
                    evaluation.IsAllowed,
                    evaluation.DenialReason,
                    evaluation.RemainingDailyLimit));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("EvaluateEnvelopeCardSpend")
            .WithOpenApi();
    }
}
