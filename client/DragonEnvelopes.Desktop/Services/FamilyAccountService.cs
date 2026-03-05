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
            "families",
            new CreateFamilyRequest(request.FamilyName),
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

        using var addMemberResponse = await PostAsync(
            $"families/{createdFamily.Id}/members",
            new AddFamilyMemberRequest(
                request.Email,
                request.PrimaryGuardianName,
                request.Email,
                "Parent"),
            cancellationToken);

        if (!addMemberResponse.IsSuccessStatusCode)
        {
            return new FamilyAccountCreateResult(
                false,
                "Family was created, but primary member setup failed. Please contact support.");
        }

        return new FamilyAccountCreateResult(
            true,
            $"Family account created successfully for {request.FamilyName}.",
            createdFamily.Id);
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
}
