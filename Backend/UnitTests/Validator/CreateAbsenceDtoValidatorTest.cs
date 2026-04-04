using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class CreateAbsenceDtoValidatorTest
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var validator = new CreateAbsenceDtoValidator();
        var dto = CreateValidDto();

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_StartDateInPast_HasError()
    {
        var validator = new CreateAbsenceDtoValidator();
        var dto = CreateValidDto();
        dto.StartDate = DateTime.Now.Date.AddDays(-1);
        dto.EndDate = DateTime.Now.Date.AddDays(1);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateAbsenceDto.StartDate));
    }

    [Fact]
    public void Validate_EndDateEqualToStartDate_HasError()
    {
        var validator = new CreateAbsenceDtoValidator();
        var dto = CreateValidDto();
        dto.StartDate = DateTime.Now.Date.AddDays(1);
        dto.EndDate = dto.StartDate;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateAbsenceDto.EndDate));
    }

    [Fact]
    public void Validate_EndDateBeforeStartDate_HasError()
    {
        var validator = new CreateAbsenceDtoValidator();
        var dto = CreateValidDto();
        dto.StartDate = DateTime.Now.Date.AddDays(2);
        dto.EndDate = DateTime.Now.Date.AddDays(1);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateAbsenceDto.EndDate));
    }

    [Fact]
    public void Validate_AbsenceTypeNull_HasError()
    {
        var validator = new CreateAbsenceDtoValidator();
        var dto = CreateValidDto();
        dto.AbsenceType = null!;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateAbsenceDto.AbsenceType));
    }

    [Fact]
    public void Validate_AbsenceTypeEmpty_HasError()
    {
        var validator = new CreateAbsenceDtoValidator();
        var dto = CreateValidDto();
        dto.AbsenceType = string.Empty;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateAbsenceDto.AbsenceType));
    }

    [Fact]
    public void Validate_AbsenceTypeInvalid_HasError()
    {
        var validator = new CreateAbsenceDtoValidator();
        var dto = CreateValidDto();
        dto.AbsenceType = "InvalidType";

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateAbsenceDto.AbsenceType));
    }

    [Fact]
    public void Validate_MessageTooShort_HasError()
    {
        var validator = new CreateAbsenceDtoValidator();
        var dto = CreateValidDto();
        dto.Message = "Hi";

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateAbsenceDto.Message));
    }

    [Fact]
    public void Validate_MessageTooLong_HasError()
    {
        var validator = new CreateAbsenceDtoValidator();
        var dto = CreateValidDto();
        dto.Message = new string('M', 251);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateAbsenceDto.Message));
    }

    [Fact]
    public void Validate_MessageNull_HasNoErrorsForMessage()
    {
        var validator = new CreateAbsenceDtoValidator();
        var dto = CreateValidDto();
        dto.Message = null;

        var result = validator.Validate(dto);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(CreateAbsenceDto.Message));
    }

    private static CreateAbsenceDto CreateValidDto()
    {
        var startDate = DateTime.Now.Date.AddDays(1);

        return new CreateAbsenceDto
        {
            StartDate = startDate,
            EndDate = startDate.AddDays(1),
            AbsenceType = nameof(AbsenceTypeEnum.Vacation),
            Message = "Trip to see family."
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
