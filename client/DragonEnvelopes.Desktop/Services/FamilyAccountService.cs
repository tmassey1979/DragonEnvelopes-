using System.Net.Http;
using System.Text;
using System.Text.Json;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Desktop.Api;

namespace DragonEnvelopes.Desktop.Services;

public sealed class FamilyAccountService(IBackendApiClient apiClient) : IFamilyAccountService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<FamilyAccountCreateResult> CreateAsync(
        CreateFamilyAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        using var createFamilyResponse = await PostAsync(
            "families/onboard",
            new CompleteFamilyOnboardingRequest(
                request.FamilyName,
                request.PrimaryGuardianFirstName,
                request.PrimaryGuardianLastName,
                request.Email,
                request.Password),
            cancellationToken);

        if (!createFamilyResponse.IsSuccessStatusCode)
        {
            return new FamilyAccountCreateResult(false, "Unable to create family. Please try again.");
        }

        var createdFamily = await DeserializeAsync<FamilyResponse>(createFamilyResponse, cancellationToken);
        if (createdFamily is null)
        {
            return new FamilyAccountCreateResult(false, "Family was created but response payload was invalid.");
        }

        return new FamilyAccountCreateResult(
            true,
            $"Family account created successfully for {request.FamilyName}.",
            createdFamily.Id);
    }

    public async Task<FamilyInviteRedemptionResult> RedeemInviteAsync(
        string inviteToken,
        string? memberName = null,
        string? memberEmail = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inviteToken))
        {
            return new FamilyInviteRedemptionResult(false, "Invite token is required.");
        }

        using var response = await PostAsync(
            "families/invites/redeem",
            new RedeemFamilyInviteRequest(inviteToken.Trim(), memberName, memberEmail),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadErrorDetailAsync(response, cancellationToken);
            return new FamilyInviteRedemptionResult(
                false,
                string.IsNullOrWhiteSpace(detail)
                    ? "Unable to redeem invite. Confirm token and account details."
                    : detail);
        }

        var redeemed = await DeserializeAsync<RedeemFamilyInviteResponse>(response, cancellationToken);
        if (redeemed is null)
        {
            return new FamilyInviteRedemptionResult(false, "Invite redeemed but response payload was invalid.");
        }

        var actionMessage = redeemed.CreatedNewMember
            ? "You were added to the family."
            : "Your account was already linked to this family.";

        return new FamilyInviteRedemptionResult(
            true,
            actionMessage,
            redeemed.Invite.FamilyId,
            redeemed.CreatedNewMember);
    }

    public async Task<FamilyInviteRedemptionResult> RegisterFromInviteAsync(
        RegisterFamilyInviteAccountRequestData request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return new FamilyInviteRedemptionResult(false, "Invite registration payload is required.");
        }

        if (string.IsNullOrWhiteSpace(request.InviteToken))
        {
            return new FamilyInviteRedemptionResult(false, "Invite token is required.");
        }

        using var response = await PostAsync(
            "families/invites/register",
            new RegisterFamilyInviteAccountRequest(
                request.InviteToken.Trim(),
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadErrorDetailAsync(response, cancellationToken);
            return new FamilyInviteRedemptionResult(
                false,
                string.IsNullOrWhiteSpace(detail)
                    ? "Unable to register from invite. Confirm token and credentials."
                    : detail);
        }

        var registered = await DeserializeAsync<RegisterFamilyInviteAccountResponse>(response, cancellationToken);
        if (registered is null)
        {
            return new FamilyInviteRedemptionResult(false, "Invite registration succeeded but response payload was invalid.");
        }

        var actionMessage = registered.CreatedNewMember
            ? "Invite account created and linked to family."
            : "Account already linked to family.";

        return new FamilyInviteRedemptionResult(
            true,
            actionMessage,
            registered.Invite.FamilyId,
            registered.CreatedNewMember);
    }

    private async Task<HttpResponseMessage> PostAsync(
        string relativePath,
        object payload,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, relativePath)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await apiClient.SendAsync(request, cancellationToken);
    }

    private static async Task<T?> DeserializeAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<T>(json, SerializerOptions);
    }

    private static async Task<string> ReadErrorDetailAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (document.RootElement.TryGetProperty("detail", out var detailElement))
                {
                    return detailElement.GetString() ?? payload;
                }

                if (document.RootElement.TryGetProperty("title", out var titleElement))
                {
                    return titleElement.GetString() ?? payload;
                }
            }
        }
        catch (JsonException)
        {
            // Fallback to the raw payload for non-JSON errors.
        }

        return payload;
    }
}
