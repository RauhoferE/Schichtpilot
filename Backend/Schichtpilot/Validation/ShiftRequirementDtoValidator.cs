using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

public class ShiftRequirementDtoValidator : AbstractValidator<ShiftRequirementDto>
{
    public ShiftRequirementDtoValidator()
    {
        RuleFor(x => x.RequiredStaffCount).GreaterThan(0);
    }
}