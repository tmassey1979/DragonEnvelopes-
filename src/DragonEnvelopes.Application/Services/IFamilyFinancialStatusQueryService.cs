using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IFamilyFinancialStatusQueryService
{
    Task<FamilyFinancialProfileDetails> GetStatusAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);
}
