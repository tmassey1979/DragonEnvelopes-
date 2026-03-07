using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace DragonEnvelopes.Api.Bootstrap;

internal static partial class ApiBootstrap
{
    public static void ConfigureAuthenticationAndAuthorization(
        WebApplicationBuilder builder,
        string authority,
        string audience,
        string[] validIssuers,
        IReadOnlyCollection<string> allowedAuthorizedParties)
    {
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidIssuers = validIssuers,
                    NameClaimType = "preferred_username",
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal is null
                            || !IsTokenIntendedForApi(context.Principal, audience, allowedAuthorizedParties))
                        {
                            context.Fail("Token audience/authorized party is not allowed for this API.");
                            return Task.CompletedTask;
                        }

                        if (context.Principal is not null)
                        {
                            KeycloakRoleClaimsTransformer.AddRoleClaims(context.Principal, audience);
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(ApiAuthorizationPolicies.Parent, policy => policy.RequireRole("Parent"));
            options.AddPolicy(ApiAuthorizationPolicies.Adult, policy => policy.RequireRole("Adult"));
            options.AddPolicy(ApiAuthorizationPolicies.Teen, policy => policy.RequireRole("Teen"));
            options.AddPolicy(ApiAuthorizationPolicies.Child, policy => policy.RequireRole("Child"));
            options.AddPolicy(ApiAuthorizationPolicies.ParentOrAdult, policy => policy.RequireRole("Parent", "Adult"));
            options.AddPolicy(ApiAuthorizationPolicies.TeenOrAbove, policy => policy.RequireRole("Parent", "Adult", "Teen"));
            options.AddPolicy(ApiAuthorizationPolicies.AnyFamilyMember, policy => policy.RequireRole(ApiAuthorizationPolicies.FamilyRoles));
        });
    }

    public static string[] BuildValidIssuers(string authority, string? publicAuthority)
    {
        static string NormalizeIssuer(string value) => value.TrimEnd('/');

        var issuers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            NormalizeIssuer(authority)
        };

        if (!string.IsNullOrWhiteSpace(publicAuthority))
        {
            issuers.Add(NormalizeIssuer(publicAuthority));
        }

        return issuers.ToArray();
    }

    private static bool IsTokenIntendedForApi(
        ClaimsPrincipal principal,
        string requiredAudience,
        IReadOnlyCollection<string> allowedAuthorizedParties)
    {
        var audiences = principal.FindAll("aud")
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (audiences.Contains(requiredAudience))
        {
            return true;
        }

        var authorizedParty = principal.FindFirst("azp")?.Value;
        return !string.IsNullOrWhiteSpace(authorizedParty)
            && allowedAuthorizedParties.Contains(authorizedParty, StringComparer.OrdinalIgnoreCase);
    }
}
