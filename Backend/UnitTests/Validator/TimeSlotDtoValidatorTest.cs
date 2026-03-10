using Schichtpilot.Models.DTOs;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class TimeSlotDtoValidatorTest
{
    [Fact]
    public void Validate_ValidTimeSlot_HasNoErrors()
    {
        var validator = new TimeSlotDtoValidator();
        var dto = CreateValidDto();

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EndTimeBeforeStartTime_HasError()
    {
        var validator = new TimeSlotDtoValidator();
        var dto = CreateValidDto();
        dto.StartTime = new TimeOnly(10, 0);
        dto.EndTime = new TimeOnly(9, 0);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(TimeSlotDto.EndTime));
    }

    [Fact]
    public void Validate_EndTimeEqualToStartTime_HasError()
    {
        var validator = new TimeSlotDtoValidator();
        var dto = CreateValidDto();
        dto.StartTime = new TimeOnly(10, 0);
        dto.EndTime = new TimeOnly(10, 0);

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(TimeSlotDto.EndTime));
    }

    [Fact]
    public void Validate_BreaksOverlapping_HasError()
    {
        var validator = new TimeSlotDtoValidator();
        var dto = CreateValidDto();
        dto.Breaks = new List<BreakDto>
        {
            new()
            {
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30)
            },
            new()
            {
                StartTime = new TimeOnly(10, 15),
                EndTime = new TimeOnly(10, 45)
            }
        };

        var result = validator.Validate(dto);

        AssertHasError(result, nameof(TimeSlotDto.Breaks));
    }

    [Fact]
    public void Validate_BreakStartAfterEnd_HasError()
    {
        var validator = new TimeSlotDtoValidator();
        var dto = CreateValidDto();
        dto.Breaks = new List<BreakDto>
        {
            new()
            {
                StartTime = new TimeOnly(10, 30),
                EndTime = new TimeOnly(10, 0)
            }
        };

        var result = validator.Validate(dto);

        AssertHasErrorStartsWith(result, nameof(TimeSlotDto.Breaks));
    }

    [Fact]
    public void Validate_BreakStartsBeforeSlot_HasError()
    {
        var validator = new TimeSlotDtoValidator();
        var dto = CreateValidDto();
        dto.StartTime = new TimeOnly(9, 0);
        dto.EndTime = new TimeOnly(12, 0);
        dto.Breaks = new List<BreakDto>
        {
            new()
            {
                StartTime = new TimeOnly(8, 30),
                EndTime = new TimeOnly(9, 15)
            }
        };

        var result = validator.Validate(dto);

        AssertHasErrorStartsWith(result, nameof(TimeSlotDto.Breaks));
    }

    [Fact]
    public void Validate_BreakEndsAfterSlot_HasError()
    {
        var validator = new TimeSlotDtoValidator();
        var dto = CreateValidDto();
        dto.StartTime = new TimeOnly(9, 0);
        dto.EndTime = new TimeOnly(12, 0);
        dto.Breaks = new List<BreakDto>
        {
            new()
            {
                StartTime = new TimeOnly(11, 30),
                EndTime = new TimeOnly(12, 30)
            }
        };

        var result = validator.Validate(dto);

        AssertHasErrorStartsWith(result, nameof(TimeSlotDto.Breaks));
    }

    private static TimeSlotDto CreateValidDto()
    {
        return new TimeSlotDto
        {
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            Breaks = new List<BreakDto>
            {
                new()
                {
                    StartTime = new TimeOnly(12, 0),
                    EndTime = new TimeOnly(12, 30)
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
