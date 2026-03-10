using Schichtpilot.Models.DTOs;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class GenerateScheduleDtoValidatorTest
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var validator = new GenerateScheduleDtoValidator();
        var dto = CreateValidDto();

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_NameNull_HasError()
    {
        var validator = new GenerateScheduleDtoValidator();
        var dto = CreateValidDto();
        dto.Name = null!;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(GenerateScheduleDto.Name));
    }

    [Fact]
    public void Validate_NameEmpty_HasError()
    {
        var validator = new GenerateScheduleDtoValidator();
        var dto = CreateValidDto();
        dto.Name = string.Empty;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(GenerateScheduleDto.Name));
    }

    [Fact]
    public void Validate_NameTooShort_HasError()
    {
        var validator = new GenerateScheduleDtoValidator();
        var dto = CreateValidDto();
        dto.Name = "AB";

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(GenerateScheduleDto.Name));
    }

    [Fact]
    public void Validate_NameTooLong_HasError()
    {
        var validator = new GenerateScheduleDtoValidator();
        var dto = CreateValidDto();
        dto.Name = new string('N', 26);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(GenerateScheduleDto.Name));
    }

    [Fact]
    public void Validate_StartDateInPast_HasError()
    {
        var validator = new GenerateScheduleDtoValidator();
        var dto = CreateValidDto();
        dto.StartDate = DateTime.Now.AddMinutes(-1);
        dto.EndDate = DateTime.Now.AddDays(1);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(GenerateScheduleDto.StartDate));
    }

    [Fact]
    public void Validate_StartDateNowOrEarlier_HasError()
    {
        var validator = new GenerateScheduleDtoValidator();
        var dto = CreateValidDto();
        dto.StartDate = DateTime.Now.AddDays(-1);
        dto.EndDate = DateTime.Now.AddDays(1);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(GenerateScheduleDto.StartDate));
    }

    [Fact]
    public void Validate_EndDateEqualToStartDate_HasError()
    {
        var validator = new GenerateScheduleDtoValidator();
        var dto = CreateValidDto();
        dto.StartDate = DateTime.Now.AddDays(1);
        dto.EndDate = dto.StartDate;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(GenerateScheduleDto.EndDate));
    }

    [Fact]
    public void Validate_EndDateBeforeStartDate_HasError()
    {
        var validator = new GenerateScheduleDtoValidator();
        var dto = CreateValidDto();
        dto.StartDate = DateTime.Now.AddDays(2);
        dto.EndDate = DateTime.Now.AddDays(1);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(GenerateScheduleDto.EndDate));
    }

    [Fact]
    public void Validate_ShiftIdsNull_HasError()
    {
        var validator = new GenerateScheduleDtoValidator();
        var dto = CreateValidDto();
        dto.ShiftIds = null!;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(GenerateScheduleDto.ShiftIds));
    }

    [Fact]
    public void Validate_ShiftIdsEmpty_HasError()
    {
        var validator = new GenerateScheduleDtoValidator();
        var dto = CreateValidDto();
        dto.ShiftIds = new List<int>();

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(GenerateScheduleDto.ShiftIds));
    }

    private static GenerateScheduleDto CreateValidDto()
    {
        var startDate = DateTime.Now.AddDays(1);

        return new GenerateScheduleDto
        {
            Name = "Weekly Plan",
            StartDate = startDate,
            EndDate = startDate.AddDays(7),
            ShiftIds = new List<int> { 1, 2, 3 }
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
