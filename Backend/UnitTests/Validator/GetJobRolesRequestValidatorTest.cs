using Schichtpilot.Models.Requests;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class GetJobRolesRequestValidatorTest
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var validator = new GetJobRolesRequestValidator();
        var request = CreateValidRequest();

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_PageTooLow_HasError()
    {
        var validator = new GetJobRolesRequestValidator();
        var request = CreateValidRequest();
        request.Page = 0;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetJobRolesRequest.Page));
    }

    [Fact]
    public void Validate_PageSizeTooLow_HasError()
    {
        var validator = new GetJobRolesRequestValidator();
        var request = CreateValidRequest();
        request.PageSize = 0;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetJobRolesRequest.PageSize));
    }

    [Fact]
    public void Validate_PageSizeTooHigh_HasError()
    {
        var validator = new GetJobRolesRequestValidator();
        var request = CreateValidRequest();
        request.PageSize = 101;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetJobRolesRequest.PageSize));
    }

    private static GetJobRolesRequest CreateValidRequest()
    {
        return new GetJobRolesRequest
        {
            Page = 1,
            PageSize = 10,
            Searchstring = null
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
