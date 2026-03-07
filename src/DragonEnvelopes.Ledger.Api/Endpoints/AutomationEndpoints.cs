namespace DragonEnvelopes.Ledger.Api.Endpoints;

internal static partial class AutomationEndpoints
{
    public static RouteGroupBuilder MapAutomationEndpoints(this RouteGroupBuilder v1)
    {
        MapAutomationRuleCrudEndpoints(v1);
        MapAutomationRuleStateEndpoints(v1);

        return v1;
    }
}
