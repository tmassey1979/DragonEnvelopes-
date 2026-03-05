namespace DragonEnvelopes.Application.Services;

public interface IImportDedupService
{
    string BuildKey(
        Guid accountId,
        DateOnly occurredOn,
        decimal amount,
        string merchant,
        string description);
}
