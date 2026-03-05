using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IIncomeAllocationEngine
{
    Task<IReadOnlyList<TransactionSplitCreateDetails>> AllocateAsync(
        Guid familyId,
        string description,
        string merchant,
        decimal amount,
        string? currentCategory,
        CancellationToken cancellationToken = default);
}
