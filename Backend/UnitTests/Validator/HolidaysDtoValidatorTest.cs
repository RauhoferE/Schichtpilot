using Schichtpilot.Models.DTOs;
using Schichtpilot.Validation;
using Xunit;

namespace UnitTests.Validator;

public class HolidaysDtoValidatorTest
{
    [Fact]
    public void Validate_Valid_HasNoErrors()
    {
        var validator = new HolidaysDtoValidator();
        var dto = new HolidaysDto { Holidays = new List<DateTime> { DateTime.UtcNow.Date } };

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyList_HasError()
    {
        var validator = new HolidaysDtoValidator();
        var dto = new HolidaysDto { Holidays = new List<DateTime>() };

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Holidays list is empty"));
    }
}
