using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Interfaces;

public interface IOnboardingBootstrapRepository
{
    Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> ListAccountNamesAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> ListEnvelopeNamesAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<bool> BudgetExistsAsync(Guid familyId, BudgetMonth month, CancellationToken cancellationToken = default);

    Task SaveBootstrapAsync(
        IReadOnlyList<Account> accounts,
        IReadOnlyList<Envelope> envelopes,
        Budget? budget,
        CancellationToken cancellationToken = default);
}
