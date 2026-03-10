using Schichtpilot.Models.DTOs;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class AddressDtoValidatorTest
{
    [Fact]
    public void Validate_ValidAddress_HasNoErrors()
    {
        var validator = new AddressDtoValidator();
        var dto = CreateValidAddress();

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_PostalCodeTooLow_HasError()
    {
        var validator = new AddressDtoValidator();
        var dto = CreateValidAddress();
        dto.PostalCode = 999;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(AddressDto.PostalCode));
    }

    [Fact]
    public void Validate_PostalCodeTooHigh_HasError()
    {
        var validator = new AddressDtoValidator();
        var dto = CreateValidAddress();
        dto.PostalCode = 10000;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(AddressDto.PostalCode));
    }

    [Fact]
    public void Validate_CityEmpty_HasError()
    {
        var validator = new AddressDtoValidator();
        var dto = CreateValidAddress();
        dto.City = string.Empty;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(AddressDto.City));
    }

    [Fact]
    public void Validate_CityTooShort_HasError()
    {
        var validator = new AddressDtoValidator();
        var dto = CreateValidAddress();
        dto.City = "A";

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(AddressDto.City));
    }

    [Fact]
    public void Validate_StreetEmpty_HasError()
    {
        var validator = new AddressDtoValidator();
        var dto = CreateValidAddress();
        dto.Street = string.Empty;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(AddressDto.Street));
    }

    [Fact]
    public void Validate_StreetTooShort_HasError()
    {
        var validator = new AddressDtoValidator();
        var dto = CreateValidAddress();
        dto.Street = "A";

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(AddressDto.Street));
    }

    private static AddressDto CreateValidAddress()
    {
        return new AddressDto
        {
            Street = "Main Street 1",
            City = "Testville",
            PostalCode = 1234
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
