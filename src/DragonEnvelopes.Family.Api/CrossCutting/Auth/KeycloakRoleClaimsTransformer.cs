using System.Security.Claims;
using System.Text.Json;

namespace DragonEnvelopes.Family.Api.CrossCutting.Auth;

public static class KeycloakRoleClaimsTransformer
{
    public static void AddRoleClaims(ClaimsPrincipal principal, string audience)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return;
        }

        var roleClaimType = identity.RoleClaimType;
        var existingRoles = identity.FindAll(roleClaimType)
            .Select(static claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var role in ReadRealmRoles(principal))
        {
            AddRoleClaim(identity, roleClaimType, existingRoles, role);
        }

        foreach (var role in ReadClientRoles(principal, audience))
        {
            AddRoleClaim(identity, roleClaimType, existingRoles, role);
        }
    }

    private static void AddRoleClaim(
        ClaimsIdentity identity,
        string roleClaimType,
        ISet<string> existingRoles,
        string role)
    {
        if (string.IsNullOrWhiteSpace(role) || !existingRoles.Add(role))
        {
            return;
        }

        identity.AddClaim(new Claim(roleClaimType, role));
    }

    private static IEnumerable<string> ReadRealmRoles(ClaimsPrincipal principal)
    {
        return ReadRolesFromJsonClaim(
            principal,
            "realm_access",
            static root =>
            {
                if (root.TryGetProperty("roles", out var rolesNode) && rolesNode.ValueKind == JsonValueKind.Array)
                {
                    return rolesNode.EnumerateArray()
                        .Where(static role => role.ValueKind == JsonValueKind.String)
                        .Select(static role => role.GetString())
                        .Where(static role => !string.IsNullOrWhiteSpace(role))
                        .Cast<string>();
                }

                return [];
            });
    }

    private static IEnumerable<string> ReadClientRoles(ClaimsPrincipal principal, string audience)
    {
        return ReadRolesFromJsonClaim(
            principal,
            "resource_access",
            root =>
            {
                if (string.IsNullOrWhiteSpace(audience))
                {
                    return [];
                }

                if (root.TryGetProperty(audience, out var clientNode)
                    && clientNode.ValueKind == JsonValueKind.Object
                    && clientNode.TryGetProperty("roles", out var rolesNode)
                    && rolesNode.ValueKind == JsonValueKind.Array)
                {
                    return rolesNode.EnumerateArray()
                        .Where(static role => role.ValueKind == JsonValueKind.String)
                        .Select(static role => role.GetString())
                        .Where(static role => !string.IsNullOrWhiteSpace(role))
                        .Cast<string>();
                }

                return [];
            });
    }

    private static IEnumerable<string> ReadRolesFromJsonClaim(
        ClaimsPrincipal principal,
        string claimType,
        Func<JsonElement, IEnumerable<string>> roleSelector)
    {
        var jsonClaim = principal.FindFirst(claimType)?.Value;
        if (string.IsNullOrWhiteSpace(jsonClaim))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(jsonClaim);
            return roleSelector(document.RootElement).ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
