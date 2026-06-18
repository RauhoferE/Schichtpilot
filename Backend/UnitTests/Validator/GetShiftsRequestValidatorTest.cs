using Schichtpilot.Models.Enums;
using Schichtpilot.Models.Requests;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class GetShiftsRequestValidatorTest
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var validator = new GetShiftsRequestValidator();
        var request = CreateValidRequest();

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_PageTooLow_HasError()
    {
        var validator = new GetShiftsRequestValidator();
        var request = CreateValidRequest();
        request.Page = 0;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetShiftsRequest.Page));
    }

    [Fact]
    public void Validate_PageSizeTooLow_HasError()
    {
        var validator = new GetShiftsRequestValidator();
        var request = CreateValidRequest();
        request.PageSize = 0;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetShiftsRequest.PageSize));
    }

    [Fact]
    public void Validate_PageSizeTooHigh_HasError()
    {
        var validator = new GetShiftsRequestValidator();
        var request = CreateValidRequest();
        request.PageSize = 101;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetShiftsRequest.PageSize));
    }

    [Fact]
    public void Validate_WeekDaysNull_NoErrorsForWeekDays()
    {
        var validator = new GetShiftsRequestValidator();
        var request = CreateValidRequest();
        request.WeekDays = null;

        var result = validator.Validate(request);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName.StartsWith(nameof(GetShiftsRequest.WeekDays)));
    }

    [Fact]
    public void Validate_WeekDaysEmpty_NoErrorsForWeekDays()
    {
        var validator = new GetShiftsRequestValidator();
        var request = CreateValidRequest();
        request.WeekDays = new string[0];

        var result = validator.Validate(request);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName.StartsWith(nameof(GetShiftsRequest.WeekDays)));
    }

    [Fact]
    public void Validate_WeekDaysInvalid_HasError()
    {
        var validator = new GetShiftsRequestValidator();
        var request = CreateValidRequest();
        request.WeekDays = new[] { "Funday" };

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetShiftsRequest.WeekDays));
    }

    [Fact]
    public void Validate_WeekDaysValid_HasNoErrors()
    {
        var validator = new GetShiftsRequestValidator();
        var request = CreateValidRequest();
        request.WeekDays = new[] { "Monday", "Sunday" };

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShiftStatusEnumInvalid_HasError()
    {
        var validator = new GetShiftsRequestValidator();
        var request = CreateValidRequest();
        request.ShiftStatusEnum = "NotAStatus";

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetShiftsRequest.ShiftStatusEnum));
    }

    [Fact]
    public void Validate_ShiftStatusEnumNull_HasError()
    {
        var validator = new GetShiftsRequestValidator();
        var request = CreateValidRequest();
        request.ShiftStatusEnum = null!;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetShiftsRequest.ShiftStatusEnum));
    }

    private static GetShiftsRequest CreateValidRequest()
    {
        return new GetShiftsRequest
        {
            Page = 1,
            PageSize = 10,
            WeekDays = null,
            ShiftStatusEnum = nameof(ShiftStatusEnum.InSchedule),
            Searchstring = null
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName.StartsWith(propertyName));
    }
}
