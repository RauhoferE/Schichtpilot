using Schichtpilot.Models.DTOs;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class CreateShiftDtoValidatorTest
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var validator = new CreateShiftDtoValidator();
        var dto = CreateValidDto();

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_NameNull_HasError()
    {
        var validator = new CreateShiftDtoValidator();
        var dto = CreateValidDto();
        dto.Name = null!;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateShiftDto.Name));
    }

    [Fact]
    public void Validate_NameEmpty_HasError()
    {
        var validator = new CreateShiftDtoValidator();
        var dto = CreateValidDto();
        dto.Name = string.Empty;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateShiftDto.Name));
    }

    [Fact]
    public void Validate_NameTooShort_HasError()
    {
        var validator = new CreateShiftDtoValidator();
        var dto = CreateValidDto();
        dto.Name = "AB";

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateShiftDto.Name));
    }

    [Fact]
    public void Validate_NameTooLong_HasError()
    {
        var validator = new CreateShiftDtoValidator();
        var dto = CreateValidDto();
        dto.Name = new string('N', 26);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateShiftDto.Name));
    }

    [Fact]
    public void Validate_ColorNull_HasError()
    {
        var validator = new CreateShiftDtoValidator();
        var dto = CreateValidDto();
        dto.ColorAsHex = null!;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateShiftDto.ColorAsHex));
    }

    [Fact]
    public void Validate_ColorEmpty_HasError()
    {
        var validator = new CreateShiftDtoValidator();
        var dto = CreateValidDto();
        dto.ColorAsHex = string.Empty;

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateShiftDto.ColorAsHex));
    }

    [Fact]
    public void Validate_ColorInvalid_HasError()
    {
        var validator = new CreateShiftDtoValidator();
        var dto = CreateValidDto();
        dto.ColorAsHex = "123456";

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateShiftDto.ColorAsHex));
    }

    [Fact]
    public void Validate_TimeSlotsOverlapping_HasError()
    {
        var validator = new CreateShiftDtoValidator();
        var dto = CreateValidDto();
        dto.TimeSlots = new List<TimeSlotDto>
        {
            new()
            {
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(12, 0),
                Breaks = new List<BreakDto>()
            },
            new()
            {
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(13, 0),
                Breaks = new List<BreakDto>()
            }
        };

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateShiftDto.TimeSlots));
    }

    [Fact]
    public void Validate_TimeSlotsDuplicateWeekday_HasError()
    {
        var validator = new CreateShiftDtoValidator();
        var dto = CreateValidDto();
        dto.TimeSlots = new List<TimeSlotDto>
        {
            new()
            {
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(12, 0),
                Breaks = new List<BreakDto>()
            },
            new()
            {
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(13, 0),
                EndTime = new TimeOnly(17, 0),
                Breaks = new List<BreakDto>()
            }
        };

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateShiftDto.TimeSlots));
    }

    [Fact]
    public void Validate_JobRequirementsRequiredStaffCountZero_HasError()
    {
        var validator = new CreateShiftDtoValidator();
        var dto = CreateValidDto();
        dto.JobRequirements = new List<ShiftRequirementDto>
        {
            new()
            {
                JobId = 1,
                Name = "Cashier",
                RequiredStaffCount = 0
            }
        };

        var result = validator.Validate(dto);

        AssertHasErrorStartsWith(result, nameof(CreateShiftDto.JobRequirements));
    }

    [Fact]
    public void Validate_JobRequirementsDuplicateJobId_HasError()
    {
        var validator = new CreateShiftDtoValidator();
        var dto = CreateValidDto();
        dto.JobRequirements = new List<ShiftRequirementDto>
        {
            new()
            {
                JobId = 1,
                Name = "Cashier",
                RequiredStaffCount = 2
            },
            new()
            {
                JobId = 1,
                Name = "Cashier 2",
                RequiredStaffCount = 1
            }
        };

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(CreateShiftDto.JobRequirements));
    }

    private static CreateShiftDto CreateValidDto()
    {
        return new CreateShiftDto
        {
            Name = "Morning Shift",
            ColorAsHex = "#A1B2C3",
            TimeSlots = new List<TimeSlotDto>
            {
                new()
                {
                    DayOfWeek = DayOfWeek.Tuesday,
                    StartTime = new TimeOnly(13, 0),
                    EndTime = new TimeOnly(17, 0),
                    Breaks = new List<BreakDto>()
                },
                new()
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(18, 0),
                    EndTime = new TimeOnly(20, 0),
                    Breaks = new List<BreakDto>()
                }
            },
            JobRequirements = new List<ShiftRequirementDto>
            {
                new()
                {
                    JobId = 1,
                    Name = "Cashier",
                    RequiredStaffCount = 2
                },
                new()
                {
                    JobId = 2,
                    Name = "Stock",
                    RequiredStaffCount = 1
                }
            }
        };
    }

    private static void AssertHasError(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }

    private static void AssertHasErrorStartsWith(FluentValidation.Results.ValidationResult result, string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName.StartsWith(propertyName));
    }
}
