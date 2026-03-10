using Schichtpilot.Models.Requests;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class UpdateScheduleRequestValidatorTest
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var validator = new UpdateScheduleRequestValidator();
        var request = CreateValidRequest();

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_StartTimeInPast_HasError()
    {
        var validator = new UpdateScheduleRequestValidator();
        var request = CreateValidRequest();
        request.StartTime = DateTime.Now.AddMinutes(-1);
        request.EndTime = DateTime.Now.AddMinutes(10);

        var result = validator.Validate(request);

        AssertHasError(result, nameof(UpdateScheduleRequest.StartTime));
    }

    [Fact]
    public void Validate_StartTimeNowOrEarlier_HasError()
    {
        var validator = new UpdateScheduleRequestValidator();
        var request = CreateValidRequest();
        request.StartTime = DateTime.Now.AddDays(-1);
        request.EndTime = DateTime.Now.AddMinutes(10);

        var result = validator.Validate(request);

        AssertHasError(result, nameof(UpdateScheduleRequest.StartTime));
    }

    [Fact]
    public void Validate_EndTimeEqualToStartTime_HasError()
    {
        var validator = new UpdateScheduleRequestValidator();
        var request = CreateValidRequest();
        request.StartTime = DateTime.Now.AddMinutes(10);
        request.EndTime = request.StartTime;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(UpdateScheduleRequest.EndTime));
    }

    [Fact]
    public void Validate_EndTimeBeforeStartTime_HasError()
    {
        var validator = new UpdateScheduleRequestValidator();
        var request = CreateValidRequest();
        request.StartTime = DateTime.Now.AddMinutes(20);
        request.EndTime = DateTime.Now.AddMinutes(10);

        var result = validator.Validate(request);

        AssertHasError(result, nameof(UpdateScheduleRequest.EndTime));
    }

    private static UpdateScheduleRequest CreateValidRequest()
    {
        var startTime = DateTime.Now.AddMinutes(10);

        return new UpdateScheduleRequest
        {
            StartTime = startTime,
            EndTime = startTime.AddMinutes(30)
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
