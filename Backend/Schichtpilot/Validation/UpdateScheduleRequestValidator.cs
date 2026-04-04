using FluentValidation;
using Schichtpilot.Models.Requests;

namespace Schichtpilot.Validation;

public class UpdateScheduleRequestValidator : AbstractValidator<UpdateScheduleRequest>
{
    public UpdateScheduleRequestValidator()
    {
        RuleFor(x => x.StartTime).NotNull().GreaterThan(DateTime.Now);
        RuleFor(x => x.EndTime).NotNull().GreaterThan(x => x.StartTime);
    }
}