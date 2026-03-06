using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IFamilyFinancialProfileRepository
{
    Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<FamilyFinancialProfile?> GetByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<FamilyFinancialProfile?> GetByFamilyIdForUpdateAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task AddAsync(FamilyFinancialProfile profile, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
