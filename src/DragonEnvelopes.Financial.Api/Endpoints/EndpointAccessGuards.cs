using System.Security.Claims;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Financial.Api.Endpoints;

internal static class EndpointAccessGuards
{
    public static async Task<bool> UserHasFamilyAccessAsync(
        ClaimsPrincipal user,
        Guid familyId,
        DragonEnvelopesDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var keycloakUserId = user.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(keycloakUserId))
        {
            return false;
        }

        return await dbContext.FamilyMembers
            .AsNoTracking()
            .AnyAsync(
                member => member.FamilyId == familyId && member.KeycloakUserId == keycloakUserId,
                cancellationToken);
    }
}

