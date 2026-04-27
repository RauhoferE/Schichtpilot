using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

/// <summary>
/// Validator for <see cref="ShiftRequirementDto"/>.
/// </summary>
public class ShiftRequirementDtoValidator : AbstractValidator<ShiftRequirementDto>
{
    public ShiftRequirementDtoValidator()
    {
        RuleFor(x => x.RequiredStaffCount).GreaterThan(0).WithMessage("RequiredStaffCount must be greater than 0");
    }
}