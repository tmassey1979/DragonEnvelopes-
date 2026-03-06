namespace DragonEnvelopes.Domain.Entities;

public sealed class PlaidSyncCursor
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public string? Cursor { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public PlaidSyncCursor(
        Guid id,
        Guid familyId,
        string? cursor,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Plaid sync cursor id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        Id = id;
        FamilyId = familyId;
        Cursor = NormalizeNullable(cursor);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Update(string? cursor, DateTimeOffset updatedAtUtc)
    {
        Cursor = NormalizeNullable(cursor);
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
