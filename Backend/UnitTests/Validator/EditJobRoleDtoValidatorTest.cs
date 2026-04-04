using Schichtpilot.Models.DTOs;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class EditJobRoleDtoValidatorTest
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var validator = new EditJobRoleDtoValidator();
        var dto = CreateValidDto();

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_NameNull_HasError()
    {
        var validator = new EditJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.Name = null!;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(EditJobRoleDto.Name));
    }

    [Fact]
    public void Validate_NameEmpty_HasError()
    {
        var validator = new EditJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.Name = string.Empty;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(EditJobRoleDto.Name));
    }

    [Fact]
    public void Validate_NameTooShort_HasError()
    {
        var validator = new EditJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.Name = "A";

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(EditJobRoleDto.Name));
    }

    [Fact]
    public void Validate_NameTooLong_HasError()
    {
        var validator = new EditJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.Name = new string('A', 51);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(EditJobRoleDto.Name));
    }

    [Fact]
    public void Validate_DescriptionNull_HasError()
    {
        var validator = new EditJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.Description = null!;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(EditJobRoleDto.Description));
    }

    [Fact]
    public void Validate_DescriptionEmpty_HasError()
    {
        var validator = new EditJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.Description = string.Empty;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(EditJobRoleDto.Description));
    }

    private static EditJobRoleDto CreateValidDto()
    {
        return new EditJobRoleDto
        {
            Name = "Cashier",
            Description = "Handles checkout and customer payments."
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
