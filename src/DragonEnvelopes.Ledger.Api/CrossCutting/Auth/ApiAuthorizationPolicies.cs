namespace DragonEnvelopes.Ledger.Api.CrossCutting.Auth;

public static class ApiAuthorizationPolicies
{
    public const string Parent = "ParentPolicy";
    public const string Adult = "AdultPolicy";
    public const string Teen = "TeenPolicy";
    public const string Child = "ChildPolicy";
    public const string ParentOrAdult = "ParentOrAdultPolicy";
    public const string TeenOrAbove = "TeenOrAbovePolicy";
    public const string AnyFamilyMember = "AnyFamilyMemberPolicy";

    public static readonly string[] FamilyRoles = ["Parent", "Adult", "Teen", "Child"];
}
