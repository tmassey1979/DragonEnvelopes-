namespace DragonEnvelopes.Application.Services;

public interface ICategorizationRuleEngine
{
    Task<string?> EvaluateAsync(
        Guid familyId,
        string description,
        string merchant,
        decimal amount,
        string? currentCategory,
        CancellationToken cancellationToken = default);
}
