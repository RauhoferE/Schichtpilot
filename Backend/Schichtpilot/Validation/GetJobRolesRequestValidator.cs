using FluentValidation;
using Schichtpilot.Models.Requests;

namespace Schichtpilot.Validation;

/// <summary>
/// Validator for <see cref="GetJobRolesRequest"/>.
/// </summary>
public class GetJobRolesRequestValidator : AbstractValidator<GetJobRolesRequest>
{
    public GetJobRolesRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page has to be larger than 0");
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100)
            .WithMessage("Pagesize has to be between 1 and 100");
    }
}