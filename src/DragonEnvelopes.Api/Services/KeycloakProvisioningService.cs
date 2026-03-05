using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DragonEnvelopes.Domain;

namespace DragonEnvelopes.Api.Services;

public sealed class KeycloakProvisioningService(
    HttpClient httpClient,
    KeycloakAdminOptions options) : IKeycloakProvisioningService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<string> CreateUserAsync(
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        var (firstName, lastName) = SplitName(displayName);

        var payload = new
        {
            username = email.Trim().ToLowerInvariant(),
            email = email.Trim().ToLowerInvariant(),
            enabled = true,
            emailVerified = true,
            firstName,
            lastName,
            credentials = new[]
            {
                new
                {
                    type = "password",
                    value = password,
                    temporary = false
                }
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{options.ServerUrl.TrimEnd('/')}/admin/realms/{options.Realm}/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new DomainValidationException("A Keycloak user with this email already exists.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Keycloak user creation failed: {(int)response.StatusCode} {body}");
        }

        if (response.Headers.Location is null)
        {
            throw new InvalidOperationException("Keycloak user creation did not return location header.");
        }

        var segments = response.Headers.Location.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var userId = segments.LastOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Unable to resolve Keycloak user id from response.");
        }

        return userId;
    }

    public async Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var token = await GetAdminTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{options.ServerUrl.TrimEnd('/')}/admin/realms/{options.Realm}/users/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{options.ServerUrl.TrimEnd('/')}/realms/{options.AdminRealm}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", options.AdminClientId),
                new KeyValuePair<string, string>("username", options.AdminUsername),
                new KeyValuePair<string, string>("password", options.AdminPassword)
            ])
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Keycloak token request failed: {(int)response.StatusCode} {body}");
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(payload);
        if (!doc.RootElement.TryGetProperty("access_token", out var tokenElement))
        {
            throw new InvalidOperationException("Keycloak token response missing access_token.");
        }

        var token = tokenElement.GetString();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Keycloak access token was empty.");
        }

        return token;
    }

    private static (string firstName, string lastName) SplitName(string displayName)
    {
        var parts = (displayName ?? string.Empty)
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        if (parts.Length == 1)
        {
            return (parts[0], string.Empty);
        }

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }
}
