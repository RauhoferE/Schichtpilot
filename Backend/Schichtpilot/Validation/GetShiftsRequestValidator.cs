using FluentValidation;
using Schichtpilot.Models.Enums;
using Schichtpilot.Models.Requests;

namespace Schichtpilot.Validation;

public class GetShiftsRequestValidator : AbstractValidator<GetShiftsRequest>
{
    public GetShiftsRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page has to be larger than 0");
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100)
            .WithMessage("Pagesize has to be between 1 and 100");
        RuleForEach(x => x.WeekDays).IsEnumName(typeof(DayOfWeek))
            .When(x => x.WeekDays != null && x.WeekDays.Count() > 0).WithMessage("Weekday not found");
        RuleFor(x => x.ShiftStatusEnum).NotNull().IsEnumName(typeof(ShiftStatusEnum)) .WithMessage("Status not found");
    }
}