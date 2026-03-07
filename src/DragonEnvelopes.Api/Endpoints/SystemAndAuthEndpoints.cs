using System.Reflection;
using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Contracts.Runtime;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Api.Endpoints;

internal static class SystemAndAuthEndpoints
{
    public static RouteGroupBuilder MapSystemAndAuthEndpoints(this RouteGroupBuilder v1)
    {
        v1.MapGet("/auth/me", async (
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var roles = user.FindAll(ClaimTypes.Role)
                    .Select(static claim => claim.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static role => role)
                    .ToArray();
                var userId = user.FindFirstValue("sub");
                Guid[] familyIds = [];
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    familyIds = await dbContext.FamilyMembers
                        .AsNoTracking()
                        .Where(member => member.KeycloakUserId == userId)
                        .Select(member => member.FamilyId)
                        .Distinct()
                        .OrderBy(id => id)
                        .ToArrayAsync(cancellationToken);
                }

                return Results.Ok(new
                {
                    username = user.Identity?.Name,
                    roles,
                    familyIds
                });
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetCurrentUser")
            .WithOpenApi();

        v1.MapGet("/auth/parent-only", () =>
                Results.Ok(new { message = "Parent access granted." }))
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("ParentOnlyProbe")
            .WithOpenApi();

        v1.MapGet("/system/health", () =>
                Results.Ok(new ApiHealthResponse("Healthy", DateTimeOffset.UtcNow)))
            .AllowAnonymous()
            .WithName("GetSystemHealth")
            .WithOpenApi();

        v1.MapGet("/system/version", (IHostEnvironment hostEnvironment) =>
            {
                var version = Environment.GetEnvironmentVariable("DRAGONENVELOPES_API_VERSION");
                if (string.IsNullOrWhiteSpace(version))
                {
                    version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";
                }

                return Results.Ok(new ApiVersionResponse(
                    version,
                    hostEnvironment.EnvironmentName,
                    DateTimeOffset.UtcNow));
            })
            .AllowAnonymous()
            .WithName("GetSystemVersion")
            .WithOpenApi();

        return v1;
    }
}
