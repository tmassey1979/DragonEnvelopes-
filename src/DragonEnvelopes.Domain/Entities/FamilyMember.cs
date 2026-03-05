using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Domain.Entities;

public sealed class FamilyMember
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public string KeycloakUserId { get; private set; }
    public string Name { get; private set; }
    public EmailAddress Email { get; private set; }
    public MemberRole Role { get; private set; }

    public FamilyMember(
        Guid id,
        Guid familyId,
        string keycloakUserId,
        string name,
        EmailAddress email,
        MemberRole role)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Family member id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        Id = id;
        FamilyId = familyId;
        KeycloakUserId = ValidateText(keycloakUserId, "Keycloak user id");
        Name = ValidateText(name, "Member name");
        Email = email;
        Role = role;
    }

    public void Rename(string name)
    {
        Name = ValidateText(name, "Member name");
    }

    public void ChangeEmail(EmailAddress email)
    {
        Email = email;
    }

    public void ChangeRole(MemberRole role)
    {
        Role = role;
    }

    private static string ValidateText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }
}

