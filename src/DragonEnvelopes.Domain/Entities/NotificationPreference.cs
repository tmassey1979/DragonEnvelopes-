namespace DragonEnvelopes.Domain.Entities;

public sealed class NotificationPreference
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public string UserId { get; }
    public bool EmailEnabled { get; private set; }
    public bool InAppEnabled { get; private set; }
    public bool SmsEnabled { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public NotificationPreference(
        Guid id,
        Guid familyId,
        string userId,
        bool emailEnabled,
        bool inAppEnabled,
        bool smsEnabled,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Notification preference id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        Id = id;
        FamilyId = familyId;
        UserId = NormalizeRequired(userId, "User id");
        EmailEnabled = emailEnabled;
        InAppEnabled = inAppEnabled;
        SmsEnabled = smsEnabled;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Update(
        bool emailEnabled,
        bool inAppEnabled,
        bool smsEnabled,
        DateTimeOffset updatedAtUtc)
    {
        EmailEnabled = emailEnabled;
        InAppEnabled = inAppEnabled;
        SmsEnabled = smsEnabled;
        UpdatedAtUtc = updatedAtUtc;
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
