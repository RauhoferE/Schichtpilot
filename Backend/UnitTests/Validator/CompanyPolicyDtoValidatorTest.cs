using Schichtpilot.Models.DTOs;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class CompanyPolicyDtoValidatorTest
{
    [Fact]
    public void Validate_ValidPolicy_HasNoErrors()
    {
        var validator = new CompanyPolicyDtoValidator();
        var dto = CreateValidPolicy();

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(120)]
    public void Validate_MinimumRestPeriodInMinutes_WithinRange_IsValid(int value)
    {
        var validator = new CompanyPolicyDtoValidator();
        var dto = CreateValidPolicy();
        dto.MinimumRestPeriodInMinutes = value;

        var result = validator.Validate(dto);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(CompanyPolicyDto.MinimumRestPeriodInMinutes));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(121)]
    public void Validate_MinimumRestPeriodInMinutes_OutOfRange_HasError(int value)
    {
        var validator = new CompanyPolicyDtoValidator();
        var dto = CreateValidPolicy();
        dto.MinimumRestPeriodInMinutes = value;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CompanyPolicyDto.MinimumRestPeriodInMinutes));
    }

    [Theory]
    [InlineData(30)]
    [InlineData(480)]
    public void Validate_RestPeriodThresholdInMinutes_WithinRange_IsValid(int value)
    {
        var validator = new CompanyPolicyDtoValidator();
        var dto = CreateValidPolicy();
        dto.RestPeriodThresholdInMinutes = value;

        var result = validator.Validate(dto);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(CompanyPolicyDto.RestPeriodThresholdInMinutes));
    }

    [Theory]
    [InlineData(29)]
    [InlineData(481)]
    public void Validate_RestPeriodThresholdInMinutes_OutOfRange_HasError(int value)
    {
        var validator = new CompanyPolicyDtoValidator();
        var dto = CreateValidPolicy();
        dto.RestPeriodThresholdInMinutes = value;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CompanyPolicyDto.RestPeriodThresholdInMinutes));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void Validate_MaximumConsecutiveWorkHoursPerDay_WithinRange_IsValid(int value)
    {
        var validator = new CompanyPolicyDtoValidator();
        var dto = CreateValidPolicy();
        dto.MaximumConsecutiveWorkHoursPerDay = value;

        var result = validator.Validate(dto);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(CompanyPolicyDto.MaximumConsecutiveWorkHoursPerDay));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Validate_MaximumConsecutiveWorkHoursPerDay_OutOfRange_HasError(int value)
    {
        var validator = new CompanyPolicyDtoValidator();
        var dto = CreateValidPolicy();
        dto.MaximumConsecutiveWorkHoursPerDay = value;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CompanyPolicyDto.MaximumConsecutiveWorkHoursPerDay));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    public void Validate_MaximumConsecutiveWorkHoursPerWeek_WithinRange_IsValid(int value)
    {
        var validator = new CompanyPolicyDtoValidator();
        var dto = CreateValidPolicy();
        dto.MaximumConsecutiveWorkHoursPerWeek = value;

        var result = validator.Validate(dto);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(CompanyPolicyDto.MaximumConsecutiveWorkHoursPerWeek));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Validate_MaximumConsecutiveWorkHoursPerWeek_OutOfRange_HasError(int value)
    {
        var validator = new CompanyPolicyDtoValidator();
        var dto = CreateValidPolicy();
        dto.MaximumConsecutiveWorkHoursPerWeek = value;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CompanyPolicyDto.MaximumConsecutiveWorkHoursPerWeek));
    }

    private static CompanyPolicyDto CreateValidPolicy()
    {
        return new CompanyPolicyDto
        {
            MinimumRestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 60,
            MaximumConsecutiveWorkHoursPerDay = 8,
            MaximumConsecutiveWorkHoursPerWeek = 40
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
