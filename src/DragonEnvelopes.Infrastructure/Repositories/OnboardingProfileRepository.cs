using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class OnboardingProfileRepository(DragonEnvelopesDbContext dbContext) : IOnboardingProfileRepository
{
    public Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .AsNoTracking()
            .AnyAsync(x => x.Id == familyId, cancellationToken);
    }

    public Task<OnboardingProfile?> GetByFamilyIdForUpdateAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.OnboardingProfiles
            .FirstOrDefaultAsync(x => x.FamilyId == familyId, cancellationToken);
    }

    public async Task<OnboardingMilestoneSignalsDetails> GetMilestoneSignalsAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var memberCount = await dbContext.FamilyMembers
            .AsNoTracking()
            .CountAsync(x => x.FamilyId == familyId, cancellationToken);

        var hasAccounts = await dbContext.Accounts
            .AsNoTracking()
            .AnyAsync(x => x.FamilyId == familyId, cancellationToken);

        var hasEnvelopes = await dbContext.Envelopes
            .AsNoTracking()
            .AnyAsync(x => x.FamilyId == familyId, cancellationToken);

        var hasBudget = await dbContext.Budgets
            .AsNoTracking()
            .AnyAsync(x => x.FamilyId == familyId, cancellationToken);

        var hasPlaidLinks = await dbContext.PlaidAccountLinks
            .AsNoTracking()
            .AnyAsync(x => x.FamilyId == familyId, cancellationToken);

        var hasStripeAccounts = await dbContext.EnvelopeFinancialAccounts
            .AsNoTracking()
            .AnyAsync(x => x.FamilyId == familyId, cancellationToken);

        var hasCards = await dbContext.EnvelopePaymentCards
            .AsNoTracking()
            .AnyAsync(x => x.FamilyId == familyId, cancellationToken);

        var hasAutomationRules = await dbContext.AutomationRules
            .AsNoTracking()
            .AnyAsync(x => x.FamilyId == familyId, cancellationToken);

        return new OnboardingMilestoneSignalsDetails(
            MembersCompleted: memberCount >= 2,
            AccountsCompleted: hasAccounts,
            EnvelopesCompleted: hasEnvelopes,
            BudgetCompleted: hasBudget,
            PlaidCompleted: hasPlaidLinks,
            StripeAccountsCompleted: hasStripeAccounts,
            CardsCompleted: hasCards,
            AutomationCompleted: hasAutomationRules);
    }

    public async Task AddAsync(OnboardingProfile profile, CancellationToken cancellationToken = default)
    {
        dbContext.OnboardingProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
