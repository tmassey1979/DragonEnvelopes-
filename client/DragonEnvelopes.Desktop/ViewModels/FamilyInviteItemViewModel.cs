namespace DragonEnvelopes.Desktop.ViewModels;

public sealed class FamilyInviteItemViewModel
{
    public FamilyInviteItemViewModel(
        Guid id,
        string email,
        string role,
        string status,
        string createdAtUtc,
        string expiresAtUtc)
    {
        Id = id;
        Email = email;
        Role = role;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; }
    public string Email { get; }
    public string Role { get; }
    public string Status { get; }
    public string CreatedAtUtc { get; }
    public string ExpiresAtUtc { get; }
}
