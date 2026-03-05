namespace DragonEnvelopes.Desktop.Services;

public interface IFamilyAccountService
{
    Task<FamilyAccountCreateResult> CreateAsync(
        CreateFamilyAccountRequest request,
        CancellationToken cancellationToken = default);
}
