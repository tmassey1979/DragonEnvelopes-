namespace DragonEnvelopes.Domain.Entities;

public sealed class FamilyInvite
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public string Email { get; private set; }
    public string Role { get; private set; }
    public string TokenHash { get; private set; }
    public FamilyInviteStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public FamilyInvite(
        Guid id,
        Guid familyId,
        string email,
        string role,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Invite id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new DomainValidationException("Invite expiry must be after creation time.");
        }

        Id = id;
        FamilyId = familyId;
        Email = NormalizeEmail(email);
        Role = NormalizeRequired(role, "Role");
        TokenHash = NormalizeRequired(tokenHash, "Token hash");
        Status = FamilyInviteStatus.Pending;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        if (Status != FamilyInviteStatus.Pending)
        {
            throw new DomainValidationException("Only pending invites can be cancelled.");
        }

        Status = FamilyInviteStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
    }

    public void Accept(DateTimeOffset acceptedAtUtc)
    {
        EnsurePendingAndNotExpired(acceptedAtUtc);
        Status = FamilyInviteStatus.Accepted;
        AcceptedAtUtc = acceptedAtUtc;
    }

    public void Expire(DateTimeOffset nowUtc)
    {
        if (Status != FamilyInviteStatus.Pending || nowUtc < ExpiresAtUtc)
        {
            return;
        }

        Status = FamilyInviteStatus.Expired;
    }

    private void EnsurePendingAndNotExpired(DateTimeOffset nowUtc)
    {
        if (Status != FamilyInviteStatus.Pending)
        {
            throw new DomainValidationException("Only pending invites can be accepted.");
        }

        if (nowUtc >= ExpiresAtUtc)
        {
            Status = FamilyInviteStatus.Expired;
            throw new DomainValidationException("Invite is expired.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        var trimmed = NormalizeRequired(email, "Email");
        if (!trimmed.Contains('@'))
        {
            throw new DomainValidationException("Email format is invalid.");
        }

        return trimmed.ToLowerInvariant();
    }

    private static string NormalizeRequired(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{field} is required.");
        }

        return value.Trim();
    }
}
