using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class AutomationRuleRepository(DragonEnvelopesDbContext dbContext) : IAutomationRuleRepository
{
    public async Task AddAsync(AutomationRule rule, CancellationToken cancellationToken = default)
    {
        dbContext.AutomationRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .AsNoTracking()
            .AnyAsync(x => x.Id == familyId, cancellationToken);
    }

    public Task<AutomationRule?> GetByIdAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        return dbContext.AutomationRules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ruleId, cancellationToken);
    }

    public Task<AutomationRule?> GetByIdForUpdateAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        return dbContext.AutomationRules
            .FirstOrDefaultAsync(x => x.Id == ruleId, cancellationToken);
    }

    public async Task<IReadOnlyList<AutomationRule>> ListAsync(
        Guid familyId,
        AutomationRuleType? type,
        bool? isEnabled,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.AutomationRules
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId);

        if (type.HasValue)
        {
            query = query.Where(x => x.RuleType == type.Value);
        }

        if (isEnabled.HasValue)
        {
            query = query.Where(x => x.IsEnabled == isEnabled.Value);
        }

        return await query
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task DeleteAsync(AutomationRule rule, CancellationToken cancellationToken = default)
    {
        dbContext.AutomationRules.Remove(rule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
