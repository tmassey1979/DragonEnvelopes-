namespace DragonEnvelopes.Family.Api.Services;

public sealed class KeycloakAdminOptions
{
    public string ServerUrl { get; init; } =
        Environment.GetEnvironmentVariable("KEYCLOAK_SERVER_URL")
        ?? "http://localhost:18080";

    public string Realm { get; init; } =
        Environment.GetEnvironmentVariable("KEYCLOAK_REALM")
        ?? "dragonenvelopes";

    public string AdminRealm { get; init; } =
        Environment.GetEnvironmentVariable("KEYCLOAK_ADMIN_REALM")
        ?? "master";

    public string AdminClientId { get; init; } =
        Environment.GetEnvironmentVariable("KEYCLOAK_ADMIN_CLIENT_ID")
        ?? "admin-cli";

    public string AdminUsername { get; init; } =
        Environment.GetEnvironmentVariable("KEYCLOAK_BOOTSTRAP_ADMIN_USERNAME")
        ?? "admin";

    public string AdminPassword { get; init; } =
        Environment.GetEnvironmentVariable("KEYCLOAK_BOOTSTRAP_ADMIN_PASSWORD")
        ?? "admin";
}
