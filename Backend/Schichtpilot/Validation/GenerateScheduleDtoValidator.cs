using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

public class GenerateScheduleDtoValidator : AbstractValidator<GenerateScheduleDto>
{
    public GenerateScheduleDtoValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty()
            .MinimumLength(3).MaximumLength(25);
        RuleFor(x => x.StartDate).NotNull().NotEmpty().GreaterThan(DateTime.Now);
        RuleFor(x => x.EndDate).NotNull().NotEmpty().GreaterThan(x => x.StartDate);
        RuleFor(x => x.ShiftIds).NotNull().NotEmpty();
    }
}