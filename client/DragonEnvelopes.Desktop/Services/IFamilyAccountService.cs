namespace DragonEnvelopes.Desktop.Services;

public interface IFamilyAccountService
{
    Task<FamilyAccountCreateResult> CreateAsync(
        CreateFamilyAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<FamilyInviteRedemptionResult> RedeemInviteAsync(
        string inviteToken,
        string? memberName = null,
        string? memberEmail = null,
        CancellationToken cancellationToken = default);

    Task<FamilyInviteRedemptionResult> RegisterFromInviteAsync(
        RegisterFamilyInviteAccountRequestData request,
        CancellationToken cancellationToken = default);
}
