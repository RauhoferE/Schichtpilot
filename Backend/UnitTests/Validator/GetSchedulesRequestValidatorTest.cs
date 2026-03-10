using Schichtpilot.Models.Enums;
using Schichtpilot.Models.Requests;
using Xunit;

namespace UnitTests.Validator;

public class GetSchedulesRequestValidatorTest
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var validator = new GetSchedulesRequestValidator();
        var request = CreateValidRequest();

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_PageTooLow_HasError()
    {
        var validator = new GetSchedulesRequestValidator();
        var request = CreateValidRequest();
        request.Page = 0;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetSchedulesRequest.Page));
    }

    [Fact]
    public void Validate_PageSizeTooLow_HasError()
    {
        var validator = new GetSchedulesRequestValidator();
        var request = CreateValidRequest();
        request.PageSize = 0;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetSchedulesRequest.PageSize));
    }

    [Fact]
    public void Validate_PageSizeTooHigh_HasError()
    {
        var validator = new GetSchedulesRequestValidator();
        var request = CreateValidRequest();
        request.PageSize = 101;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetSchedulesRequest.PageSize));
    }

    [Fact]
    public void Validate_StatusNull_HasError()
    {
        var validator = new GetSchedulesRequestValidator();
        var request = CreateValidRequest();
        request.Status = null!;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetSchedulesRequest.Status));
    }

    [Fact]
    public void Validate_StatusEmpty_HasError()
    {
        var validator = new GetSchedulesRequestValidator();
        var request = CreateValidRequest();
        request.Status = string.Empty;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetSchedulesRequest.Status));
    }

    [Fact]
    public void Validate_StatusInvalid_HasError()
    {
        var validator = new GetSchedulesRequestValidator();
        var request = CreateValidRequest();
        request.Status = "InvalidStatus";

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetSchedulesRequest.Status));
    }

    private static GetSchedulesRequest CreateValidRequest()
    {
        return new GetSchedulesRequest
        {
            Page = 1,
            PageSize = 10,
            StartDate = null,
            EndDate = null,
            Searchstring = null,
            ShiftIds = null,
            Status = nameof(ScheduleStatusEnum.Active)
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
