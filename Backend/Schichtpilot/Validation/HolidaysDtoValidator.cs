using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

/// <summary>
/// Validator for <see cref="HolidaysDto"/>.
/// </summary>
public class HolidaysDtoValidator : AbstractValidator<HolidaysDto>
{
    public HolidaysDtoValidator()
    {
        RuleFor(x => x).Must(x => x.Holidays.Any()).WithMessage("Holidays list is empty");
        RuleForEach(x => x.Holidays).NotNull().WithMessage("Holidays list is empty");
    }
}