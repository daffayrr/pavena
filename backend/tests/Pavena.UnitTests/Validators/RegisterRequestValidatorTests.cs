using FluentAssertions;
using Pavena.Application.Identity.DTOs;
using Pavena.Application.Identity.Validators;
using Xunit;

namespace Pavena.UnitTests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_PassesValidation()
    {
        var request = new RegisterRequest(
            "scholar@pavena.org",
            "scholar_123",
            "SecurePass#2026",
            "Dr. Scholar",
            "US"
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@nodomain.com")]
    [InlineData("spaces in@email.com")]
    public void Validate_WithInvalidEmail_FailsValidation(string email)
    {
        var request = new RegisterRequest(
            email,
            "valid_user",
            "SecurePass#2026",
            "Valid Name"
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    [Theory]
    [InlineData("short")] // Less than 8
    [InlineData("alllowercase123#")] // Missing uppercase
    [InlineData("ALLUPPERCASE123#")] // Missing lowercase
    [InlineData("NoSpecialChars123")] // Missing special char
    [InlineData("NoNumbersHere#")] // Missing number
    public void Validate_WithWeakPassword_FailsValidation(string password)
    {
        var request = new RegisterRequest(
            "valid@pavena.org",
            "valid_user",
            password,
            "Valid Name"
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Theory]
    [InlineData("ab")] // Too short (< 3)
    [InlineData("user with spaces")] // Invalid chars
    [InlineData("user!@#")] // Special chars not allowed
    public void Validate_WithInvalidUsername_FailsValidation(string username)
    {
        var request = new RegisterRequest(
            "valid@pavena.org",
            username,
            "SecurePass#2026",
            "Valid Name"
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Username));
    }
}
