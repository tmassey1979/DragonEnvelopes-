using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IFamilyService
{
    Task<FamilyDetails> CreateAsync(string name, CancellationToken cancellationToken = default);

    Task<FamilyDetails?> GetByIdAsync(Guid familyId, CancellationToken cancellationToken = default);
}
