using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IApprovalPolicyRepository
{
    Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<FamilyApprovalPolicy?> GetByFamilyIdAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<FamilyApprovalPolicy?> GetByFamilyIdForUpdateAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        FamilyApprovalPolicy policy,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
