using System;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Requests;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class CreateUserRequestValidatorTest
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

        AssertHasError(result, nameof(CreateUserRequest.Email));
    }

    [Fact]
    public void Validate_EmailEmpty_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.Email = string.Empty;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(CreateUserRequest.Email));
    }

    [Fact]
    public void Validate_EmailInvalidFormat_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.Email = "not-an-email";

        var result = validator.Validate(request);

        AssertHasError(result, nameof(CreateUserRequest.Email));
    }

    [Fact]
    public void Validate_PasswordMissing_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.Password = null!;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(CreateUserRequest.Password));
    }

    [Fact]
    public void Validate_PasswordEmpty_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.Password = string.Empty;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(CreateUserRequest.Password));
    }

    [Theory]
    [InlineData("short1!")] // too short, missing uppercase
    [InlineData("alllowercase1!")] // missing uppercase
    [InlineData("ALLUPPERCASE1!")] // missing lowercase
    [InlineData("NoNumber!")] // missing number
    [InlineData("NoSpecial1")] // missing special
    public void Validate_PasswordDoesNotMeetRequirements_HasError(string password)
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.Password = password;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(CreateUserRequest.Password));
    }

    [Fact]
    public void Validate_BirthdateInFuture_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.Birthdate = DateTime.Now.AddDays(1);

        var result = validator.Validate(request);

        AssertHasError(result, nameof(CreateUserRequest.Birthdate));
    }

    [Fact]
    public void Validate_FirstNameMissing_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.FirstName = null!;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(CreateUserRequest.FirstName));
    }

    [Fact]
    public void Validate_FirstNameTooShort_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.FirstName = "Al";

        var result = validator.Validate(request);

        AssertHasError(result, nameof(CreateUserRequest.FirstName));
    }

    [Fact]
    public void Validate_FirstNameTooLong_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.FirstName = new string('A', 21);

        var result = validator.Validate(request);

        AssertHasError(result, nameof(CreateUserRequest.FirstName));
    }

    [Fact]
    public void Validate_LastNameMissing_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.LastName = null!;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(CreateUserRequest.LastName));
    }

    [Fact]
    public void Validate_LastNameTooShort_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.LastName = "Li";

        var result = validator.Validate(request);

        AssertHasError(result, nameof(CreateUserRequest.LastName));
    }

    [Fact]
    public void Validate_LastNameTooLong_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.LastName = new string('B', 21);

        var result = validator.Validate(request);

        AssertHasError(result, nameof(CreateUserRequest.LastName));
    }

    [Fact]
    public void Validate_AddressMissing_HasError()
    {
        var validator = CreateValidator();
        var request = CreateValidRequest();
        request.AddressDto = null!;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(CreateUserRequest.AddressDto));
    }

    private static CreateUserRequestValidator CreateValidator()
    {
        return new CreateUserRequestValidator(new AddressDtoValidator());
    }

    private static CreateUserRequest CreateValidRequest()
    {
        return new CreateUserRequest
        {
            Email = "user@test.com",
            Password = "StrongPass1!",
            FirstName = "John",
            LastName = "Doe",
            Birthdate = DateTime.Now.AddYears(-20),
            AddressDto = new AddressDto
            {
                Street = "Main Street 1",
                City = "Testville",
                PostalCode = 1234
            }
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
