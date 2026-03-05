using System.Net.Mail;

namespace DragonEnvelopes.Domain.ValueObjects;

public sealed record EmailAddress
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("Email is required.");
        }

        try
        {
            var parsed = new MailAddress(value.Trim());
            return new EmailAddress(parsed.Address.ToLowerInvariant());
        }
        catch (FormatException)
        {
            throw new DomainValidationException("Email format is invalid.");
        }
    }

    public override string ToString() => Value;
}

