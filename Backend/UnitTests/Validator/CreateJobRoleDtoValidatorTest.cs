using Schichtpilot.Models.DTOs;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class CreateJobRoleDtoValidatorTest
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var validator = new CreateJobRoleDtoValidator();
        var dto = CreateValidDto();

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_NameNull_HasError()
    {
        var validator = new CreateJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.Name = null!;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateJobRoleDto.Name));
    }

    [Fact]
    public void Validate_NameEmpty_HasError()
    {
        var validator = new CreateJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.Name = string.Empty;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateJobRoleDto.Name));
    }

    [Fact]
    public void Validate_NameTooShort_HasError()
    {
        var validator = new CreateJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.Name = "A";

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateJobRoleDto.Name));
    }

    [Fact]
    public void Validate_NameTooLong_HasError()
    {
        var validator = new CreateJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.Name = new string('A', 51);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateJobRoleDto.Name));
    }

    [Fact]
    public void Validate_DescriptionNull_HasError()
    {
        var validator = new CreateJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.Description = null!;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateJobRoleDto.Description));
    }

    [Fact]
    public void Validate_DescriptionEmpty_HasError()
    {
        var validator = new CreateJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.Description = string.Empty;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateJobRoleDto.Description));
    }

    [Fact]
    public void Validate_DescriptionTooShort_HasError()
    {
        var validator = new CreateJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.Description = "A";

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateJobRoleDto.Description));
    }

    [Fact]
    public void Validate_DescriptionTooLong_HasError()
    {
        var validator = new CreateJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.Description = new string('D', 251);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateJobRoleDto.Description));
    }

    [Fact]
    public void Validate_DuplicateDependentJobRoleIds_HasError()
    {
        var validator = new CreateJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.DependentOnJobRoleIds = new List<int> { 1, 2, 2 };

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateJobRoleDto.DependentOnJobRoleIds));
    }

    [Fact]
    public void Validate_UniqueDependentJobRoleIds_HasNoErrorsForIds()
    {
        var validator = new CreateJobRoleDtoValidator();
        var dto = CreateValidDto();
        dto.DependentOnJobRoleIds = new List<int> { 1, 2, 3 };

        var result = validator.Validate(dto);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(CreateJobRoleDto.DependentOnJobRoleIds));
    }

    private static CreateJobRoleDto CreateValidDto()
    {
        return new CreateJobRoleDto
        {
            Name = "Cashier",
            Description = "Handles checkout and customer payments.",
            DependentOnJobRoleIds = new List<int>()
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
