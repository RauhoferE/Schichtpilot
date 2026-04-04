using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

public class CompanyPolicyDtoValidator : AbstractValidator<CompanyPolicyDto>
{
    public CompanyPolicyDtoValidator()
    {
        RuleFor(policy => policy.MinimumRestPeriodInMinutes).InclusiveBetween(5, 120);
        RuleFor(policy => policy.RestPeriodThresholdInMinutes).InclusiveBetween(30, 480);
        RuleFor(policy => policy.MaximumConsecutiveWorkHoursPerDay).InclusiveBetween(1, 12);
        RuleFor(policy => policy.MaximumConsecutiveWorkHoursPerWeek).InclusiveBetween(1, 60);
    }
}