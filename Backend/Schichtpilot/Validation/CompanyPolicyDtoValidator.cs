using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

/// <summary>
/// Validator for <see cref="CompanyPolicyDto"/>.
/// </summary>
public class CompanyPolicyDtoValidator : AbstractValidator<CompanyPolicyDto>
{
    public CompanyPolicyDtoValidator()
    {
        RuleFor(policy => policy.MinimumRestPeriodInMinutes).InclusiveBetween(5, 120).WithMessage("The rest period must be between 5 and 120 minutes.");
        RuleFor(policy => policy.RestPeriodThresholdInMinutes).InclusiveBetween(30, 480).WithMessage("The rest period threshold must be between 30 and 480 minutes.");
        RuleFor(policy => policy.MaximumConsecutiveWorkHoursPerDay).InclusiveBetween(1, 12).WithMessage("The maximum consecutive work hours per day must be between 1 and 12 hours.");
        RuleFor(policy => policy.MaximumConsecutiveWorkHoursPerWeek).InclusiveBetween(1, 60).WithMessage("The maximum consecutive work hours per week must be between 1 and 60 hours.");
    }
}