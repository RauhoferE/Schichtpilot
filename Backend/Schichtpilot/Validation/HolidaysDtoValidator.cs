using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

public class HolidaysDtoValidator : AbstractValidator<HolidaysDto>
{
    public HolidaysDtoValidator()
    {
        RuleFor(x => x).Must(x => x.Holidays.Any());
        RuleForEach(x => x.Holidays).NotNull();
    }
}