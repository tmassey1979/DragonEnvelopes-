namespace DragonEnvelopes.Api.Endpoints;

internal static partial class FamilyEndpoints
{
    public static RouteGroupBuilder MapFamilyEndpoints(this RouteGroupBuilder v1)
    {
        MapFamilyLifecycleEndpoints(v1);
        MapFamilyMembershipAndInviteEndpoints(v1);
        MapFamilyOnboardingEndpoints(v1);

        return v1;
    }
}
