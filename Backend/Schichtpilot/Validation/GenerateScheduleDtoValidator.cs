using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

/// <summary>
/// Validator for <see cref="GenerateScheduleDto"/>.
/// </summary>
public class GenerateScheduleDtoValidator : AbstractValidator<GenerateScheduleDto>
{
    public GenerateScheduleDtoValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty()
            .MinimumLength(3).MaximumLength(25).WithMessage("Name is required and must be between 3 and 25 characters");
        RuleFor(x => x.StartDate).NotNull().NotEmpty().GreaterThan(DateTime.Now).WithMessage("Start date must be in the future");
        RuleFor(x => x.EndDate).NotNull().NotEmpty().GreaterThan(x => x.StartDate).WithMessage("End date must be later than the start date");
        RuleFor(x => x.ShiftIds).NotNull().NotEmpty().WithMessage("Shifts are required");
    }
}