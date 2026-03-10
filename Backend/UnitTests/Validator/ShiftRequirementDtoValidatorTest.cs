using Schichtpilot.Models.DTOs;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class ShiftRequirementDtoValidatorTest
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var validator = new ShiftRequirementDtoValidator();
        var dto = CreateValidDto();

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RequiredStaffCountZero_HasError()
    {
        var validator = new ShiftRequirementDtoValidator();
        var dto = CreateValidDto();
        dto.RequiredStaffCount = 0;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(ShiftRequirementDto.RequiredStaffCount));
    }

    [Fact]
    public void Validate_RequiredStaffCountNegative_HasError()
    {
        var validator = new ShiftRequirementDtoValidator();
        var dto = CreateValidDto();
        dto.RequiredStaffCount = -1;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(ShiftRequirementDto.RequiredStaffCount));
    }

    private static ShiftRequirementDto CreateValidDto()
    {
        return new ShiftRequirementDto
        {
            JobId = 1,
            Name = "Cashier",
            RequiredStaffCount = 1
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
