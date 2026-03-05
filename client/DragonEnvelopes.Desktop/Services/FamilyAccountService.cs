namespace DragonEnvelopes.Desktop.Services;

public sealed class FamilyAccountService : IFamilyAccountService
{
    public Task<FamilyAccountCreateResult> CreateAsync(
        CreateFamilyAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = request;
        _ = cancellationToken;

        return Task.FromResult(new FamilyAccountCreateResult(
            true,
            "Family account request captured. API submission will be connected when backend endpoint is available."));
    }
}
