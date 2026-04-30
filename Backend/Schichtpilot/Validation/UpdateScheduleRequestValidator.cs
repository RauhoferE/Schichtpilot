using FluentValidation;
using Schichtpilot.Models.Requests;

namespace Schichtpilot.Validation;

/// <summary>
/// Validator <see cref="UpdateScheduleRequest"/>.
/// </summary>
public class UpdateScheduleRequestValidator : AbstractValidator<UpdateScheduleRequest>
{
    public UpdateScheduleRequestValidator()
    {
        RuleFor(x => x.StartTime).NotNull().GreaterThan(DateTime.Now).WithMessage("Start time must be in the future");
        RuleFor(x => x.EndTime).NotNull().GreaterThan(x => x.StartTime).WithMessage("End time must be later than start time");
    }
}