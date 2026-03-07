namespace DragonEnvelopes.Family.Api.Endpoints;

internal static partial class FamilyEndpoints
{
    public static RouteGroupBuilder MapFamilyEndpoints(this RouteGroupBuilder v1)
    {
        return v1
            .MapFamilyCoreEndpoints()
            .MapFamilyMembersAndInvitesEndpoints()
            .MapFamilyOnboardingEndpoints();
    }
}
