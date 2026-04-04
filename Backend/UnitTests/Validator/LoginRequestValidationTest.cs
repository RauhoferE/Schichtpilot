using Schichtpilot.Models.Requests;
using Schichtpilot.Validation;

namespace UnitTests.Validator;

public class LoginRequestValidationTest
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EmailMissing_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.Email = null!;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(LoginRequest.Email));
    }

    [Fact]
    public void Validate_EmailEmpty_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.Email = string.Empty;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(LoginRequest.Email));
    }

    [Fact]
    public void Validate_EmailInvalidFormat_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.Email = "not-an-email";

        var result = validator.Validate(request);

        AssertHasError(result, nameof(LoginRequest.Email));
    }

    [Fact]
    public void Validate_PasswordMissing_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.Password = null!;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(LoginRequest.Password));
    }

    [Fact]
    public void Validate_PasswordEmpty_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.Password = string.Empty;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(LoginRequest.Password));
    }

    private static LoginRequestValidation CreateValidator()
    {
        return new LoginRequestValidation();
    }

    private static LoginRequest CreateValidRequest()
    {
        return new LoginRequest
        {
            Email = "user@test.com",
            Password = "secret"
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
