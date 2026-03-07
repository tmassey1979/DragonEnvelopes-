namespace DragonEnvelopes.Api.Endpoints;

internal static partial class PlanningAndReportingEndpoints
{
    public static RouteGroupBuilder MapPlanningAndReportingEndpoints(this RouteGroupBuilder v1)
    {
        MapEnvelopePlanningEndpoints(v1);
        MapBudgetPlanningEndpoints(v1);
        MapRecurringBillPlanningEndpoints(v1);
        MapReportingEndpoints(v1);

        return v1;
    }
}
