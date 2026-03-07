namespace DragonEnvelopes.Ledger.Api.Endpoints;

internal static partial class PlanningEndpoints
{
    public static RouteGroupBuilder MapPlanningEndpoints(this RouteGroupBuilder v1)
    {
        MapEnvelopePlanningEndpoints(v1);
        MapEnvelopeGoalPlanningEndpoints(v1);
        MapBudgetPlanningEndpoints(v1);
        MapRecurringBillPlanningEndpoints(v1);
        MapReportingEndpoints(v1);
        MapScenarioPlanningEndpoints(v1);
        MapSpendAnomalyPlanningEndpoints(v1);
        return v1;
    }
}
