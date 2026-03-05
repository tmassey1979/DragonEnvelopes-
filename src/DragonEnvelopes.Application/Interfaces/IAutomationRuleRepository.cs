using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Interfaces;

public interface IAutomationRuleRepository
{
    Task AddAsync(AutomationRule rule, CancellationToken cancellationToken = default);

    Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<AutomationRule?> GetByIdAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<AutomationRule?> GetByIdForUpdateAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AutomationRule>> ListAsync(
        Guid familyId,
        AutomationRuleType? type,
        bool? isEnabled,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(AutomationRule rule, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
