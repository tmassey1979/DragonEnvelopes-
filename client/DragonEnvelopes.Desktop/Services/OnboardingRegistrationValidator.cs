using System.Net.Mail;

namespace DragonEnvelopes.Desktop.Services;

public static class OnboardingRegistrationValidator
{
    public static string? ValidateAccountStep(
        string? firstName,
        string? lastName,
        string? email,
        string? password,
        string? confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return "Primary guardian first name is required.";
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return "Primary guardian last name is required.";
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return "Email is required.";
        }

        try
        {
            _ = new MailAddress(email.Trim());
        }
        catch (FormatException)
        {
            return "Enter a valid email address.";
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required.";
        }

        if (password.Length < 8)
        {
            return "Password must be at least 8 characters.";
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            return "Password confirmation does not match.";
        }

        return null;
    }

    public static string? ValidateFamilyStep(string? familyName)
    {
        if (string.IsNullOrWhiteSpace(familyName))
        {
            return "Family name is required.";
        }

        return null;
    }

    public static string? ValidateInviteRegistrationStep(
        string? inviteToken,
        string? firstName,
        string? lastName,
        string? email,
        string? password,
        string? confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(inviteToken))
        {
            return "Invite token is required.";
        }

        return ValidateAccountStep(firstName, lastName, email, password, confirmPassword);
    }
}
