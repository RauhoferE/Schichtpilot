using Schichtpilot.Models.Enums;
using Schichtpilot.Models.Requests;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class GetUsersRequestValidatorTest
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var validator = new GetUsersRequestValidator();
        var request = CreateValidRequest();

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_PageTooLow_HasError()
    {
        var validator = new GetUsersRequestValidator();
        var request = CreateValidRequest();
        request.Page = 0;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetUsersRequest.Page));
    }

    [Fact]
    public void Validate_PageSizeTooLow_HasError()
    {
        var validator = new GetUsersRequestValidator();
        var request = CreateValidRequest();
        request.PageSize = 0;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetUsersRequest.PageSize));
    }

    [Fact]
    public void Validate_PageSizeTooHigh_HasError()
    {
        var validator = new GetUsersRequestValidator();
        var request = CreateValidRequest();
        request.PageSize = 101;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetUsersRequest.PageSize));
    }

    [Fact]
    public void Validate_SortPropertyNull_HasError()
    {
        var validator = new GetUsersRequestValidator();
        var request = CreateValidRequest();
        request.SortProperty = null!;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetUsersRequest.SortProperty));
    }

    [Fact]
    public void Validate_SortPropertyEmpty_HasError()
    {
        var validator = new GetUsersRequestValidator();
        var request = CreateValidRequest();
        request.SortProperty = string.Empty;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetUsersRequest.SortProperty));
    }

    [Fact]
    public void Validate_SortPropertyInvalid_HasError()
    {
        var validator = new GetUsersRequestValidator();
        var request = CreateValidRequest();
        request.SortProperty = "InvalidSort";

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetUsersRequest.SortProperty));
    }

    [Fact]
    public void Validate_AccountStatusNull_HasError()
    {
        var validator = new GetUsersRequestValidator();
        var request = CreateValidRequest();
        request.AccountStatus = null!;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetUsersRequest.AccountStatus));
    }

    [Fact]
    public void Validate_AccountStatusEmpty_HasError()
    {
        var validator = new GetUsersRequestValidator();
        var request = CreateValidRequest();
        request.AccountStatus = string.Empty;

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetUsersRequest.AccountStatus));
    }

    [Fact]
    public void Validate_AccountStatusInvalid_HasError()
    {
        var validator = new GetUsersRequestValidator();
        var request = CreateValidRequest();
        request.AccountStatus = "InvalidStatus";

        var result = validator.Validate(request);

        AssertHasError(result, nameof(GetUsersRequest.AccountStatus));
    }

    private static GetUsersRequest CreateValidRequest()
    {
        return new GetUsersRequest
        {
            Page = 1,
            PageSize = 10,
            SortProperty = nameof(UserSortEnum.Id),
            Ascending = true,
            JobFilters = Array.Empty<string>(),
            AccountStatus = nameof(AccountStatusEnum.Ok),
            Searchstring = null
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
