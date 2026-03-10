using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class StatusUpdateDtoValidatorTest
{
    [Fact]
    public void Validate_ValidApprovedRequest_HasNoErrors()
    {
        var validator = new StatusUpdateDtoValidator();
        var dto = CreateApprovedDto();

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_StatusNull_HasError()
    {
        var validator = new StatusUpdateDtoValidator();
        var dto = CreateApprovedDto();
        dto.Status = null!;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(StatusUpdateDto.Status));
    }

    [Fact]
    public void Validate_StatusEmpty_HasError()
    {
        var validator = new StatusUpdateDtoValidator();
        var dto = CreateApprovedDto();
        dto.Status = string.Empty;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(StatusUpdateDto.Status));
    }

    [Fact]
    public void Validate_StatusInvalid_HasError()
    {
        var validator = new StatusUpdateDtoValidator();
        var dto = CreateApprovedDto();
        dto.Status = "InvalidStatus";

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(StatusUpdateDto.Status));
    }

    [Fact]
    public void Validate_StatusPending_HasError()
    {
        var validator = new StatusUpdateDtoValidator();
        var dto = CreateApprovedDto();
        dto.Status = nameof(AbsenceStatusEnum.Pending);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(StatusUpdateDto.Status));
    }

    [Fact]
    public void Validate_ManagerMessageNull_WhenStatusNotDenied_HasNoError()
    {
        var validator = new StatusUpdateDtoValidator();
        var dto = CreateApprovedDto();
        dto.ManagerMessage = null;

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ManagerMessageEmpty_WhenStatusNotDenied_HasError()
    {
        var validator = new StatusUpdateDtoValidator();
        var dto = CreateApprovedDto();
        dto.ManagerMessage = string.Empty;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(StatusUpdateDto.ManagerMessage));
    }

    [Fact]
    public void Validate_ManagerMessageTooShort_HasError()
    {
        var validator = new StatusUpdateDtoValidator();
        var dto = CreateApprovedDto();
        dto.ManagerMessage = "Hi";

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(StatusUpdateDto.ManagerMessage));
    }

    [Fact]
    public void Validate_ManagerMessageTooLong_HasError()
    {
        var validator = new StatusUpdateDtoValidator();
        var dto = CreateApprovedDto();
        dto.ManagerMessage = new string('M', 251);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(StatusUpdateDto.ManagerMessage));
    }

    [Fact]
    public void Validate_ManagerMessageNull_WhenStatusDenied_HasErrorsForMessage()
    {
        var validator = new StatusUpdateDtoValidator();
        var dto = CreateDeniedDto();
        dto.ManagerMessage = null;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(StatusUpdateDto.ManagerMessage));
    }

    [Fact]
    public void Validate_ManagerMessageEmpty_WhenStatusDenied_HasError()
    {
        var validator = new StatusUpdateDtoValidator();
        var dto = CreateDeniedDto();
        dto.ManagerMessage = string.Empty;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(StatusUpdateDto.ManagerMessage));
    }
    
    [Fact]
    public void Validate_ManagerMessageNotEmpty_WhenStatusDenied_HasNoError()
    {
        var validator = new StatusUpdateDtoValidator();
        var dto = CreateDeniedDto();

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    private static StatusUpdateDto CreateApprovedDto()
    {
        return new StatusUpdateDto
        {
            Status = nameof(AbsenceStatusEnum.Approved),
            ManagerMessage = "Approved"
        };
    }

    private static StatusUpdateDto CreateDeniedDto()
    {
        return new StatusUpdateDto
        {
            Status = nameof(AbsenceStatusEnum.Denied),
            ManagerMessage = "Denied"
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
