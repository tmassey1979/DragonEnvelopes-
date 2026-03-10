namespace DragonEnvelopes.Application.Services;

public static class WorkflowSagaTypes
{
    public const string Onboarding = "Onboarding";
    public const string Approval = "Approval";
}

public static class WorkflowSagaStatuses
{
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Compensated = "Compensated";
}
