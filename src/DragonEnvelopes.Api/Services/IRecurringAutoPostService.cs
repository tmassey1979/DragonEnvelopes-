namespace DragonEnvelopes.Api.Services;

public interface IRecurringAutoPostService
{
    Task<RecurringAutoPostRunSummary> RunAsync(
        Guid? familyId = null,
        DateOnly? dueDate = null,
        CancellationToken cancellationToken = default);
}
