using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class OnboardingRegistrationValidatorTests
{
    [Fact]
    public void ValidateAccountStep_ReturnsError_WhenEmailInvalid()
    {
        var error = OnboardingRegistrationValidator.ValidateAccountStep(
            firstName: "Alex",
            lastName: "Owner",
            email: "invalid-email",
            password: "password123",
            confirmPassword: "password123");

        Assert.Equal("Enter a valid email address.", error);
    }

    [Fact]
    public void ValidateAccountStep_ReturnsError_WhenPasswordMismatch()
    {
        var error = OnboardingRegistrationValidator.ValidateAccountStep(
            firstName: "Alex",
            lastName: "Owner",
            email: "alex@test.dev",
            password: "password123",
            confirmPassword: "password124");

        Assert.Equal("Password confirmation does not match.", error);
    }

    [Fact]
    public void ValidateAccountStep_ReturnsNull_WhenInputValid()
    {
        var error = OnboardingRegistrationValidator.ValidateAccountStep(
            firstName: "Alex",
            lastName: "Owner",
            email: "alex@test.dev",
            password: "password123",
            confirmPassword: "password123");

        Assert.Null(error);
    }

    [Fact]
    public void ValidateFamilyStep_ReturnsError_WhenFamilyNameMissing()
    {
        var error = OnboardingRegistrationValidator.ValidateFamilyStep(" ");

        Assert.Equal("Family name is required.", error);
    }
}
