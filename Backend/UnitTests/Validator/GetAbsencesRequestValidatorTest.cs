using Schichtpilot.Models.Enums;
using Schichtpilot.Models.Requests;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class GetAbsencesRequestValidatorTest
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var validator = new GetAbsencesRequestValidator();
        var request = CreateValidRequest();

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_PageTooLow_HasError()
    {
        var validator = new GetAbsencesRequestValidator();
        var request = CreateValidRequest();
        request.Page = 0;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetAbsencesRequest.Page));
    }

    [Fact]
    public void Validate_PageSizeTooLow_HasError()
    {
        var validator = new GetAbsencesRequestValidator();
        var request = CreateValidRequest();
        request.PageSize = 0;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetAbsencesRequest.PageSize));
    }

    [Fact]
    public void Validate_PageSizeTooHigh_HasError()
    {
        var validator = new GetAbsencesRequestValidator();
        var request = CreateValidRequest();
        request.PageSize = 101;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetAbsencesRequest.PageSize));
    }

    [Fact]
    public void Validate_StatusNull_HasNoErrorsForStatus()
    {
        var validator = new GetAbsencesRequestValidator();
        var request = CreateValidRequest();
        request.Status = null;

        var result = validator.Validate(request);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName.StartsWith(nameof(GetAbsencesRequest.Status)));
    }

    [Fact]
    public void Validate_StatusEmptyList_HasNoErrorsForStatus()
    {
        var validator = new GetAbsencesRequestValidator();
        var request = CreateValidRequest();
        request.Status = new List<string>();

        var result = validator.Validate(request);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName.StartsWith(nameof(GetAbsencesRequest.Status)));
    }

    [Fact]
    public void Validate_StatusInvalidValue_HasError()
    {
        var validator = new GetAbsencesRequestValidator();
        var request = CreateValidRequest();
        request.Status = new List<string> { "InvalidStatus" };

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetAbsencesRequest.Status));
    }

    [Fact]
    public void Validate_AbsenceTypeNull_HasNoErrorsForAbsenceType()
    {
        var validator = new GetAbsencesRequestValidator();
        var request = CreateValidRequest();
        request.AbsenceType = null;

        var result = validator.Validate(request);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName.StartsWith(nameof(GetAbsencesRequest.AbsenceType)));
    }

    [Fact]
    public void Validate_AbsenceTypeEmptyList_HasNoErrorsForAbsenceType()
    {
        var validator = new GetAbsencesRequestValidator();
        var request = CreateValidRequest();
        request.AbsenceType = new List<string>();

        var result = validator.Validate(request);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName.StartsWith(nameof(GetAbsencesRequest.AbsenceType)));
    }

    [Fact]
    public void Validate_AbsenceTypeInvalidValue_HasError()
    {
        var validator = new GetAbsencesRequestValidator();
        var request = CreateValidRequest();
        request.AbsenceType = new List<string> { "InvalidType" };

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetAbsencesRequest.AbsenceType));
    }

    private static GetAbsencesRequest CreateValidRequest()
    {
        return new GetAbsencesRequest
        {
            Page = 1,
            PageSize = 10,
            Status = new List<string> { nameof(AbsenceStatusEnum.Approved) },
            AbsenceType = new List<string> { nameof(AbsenceTypeEnum.Vacation) },
            CreatedFrom = null,
            CreatedTo = null,
            StartDateFrom = null,
            StartDateTo = null,
            Searchstring = null
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName.StartsWith(propertyName));
    }
}
