using FluentValidation;
using Schichtpilot.Models.Enums;
using Schichtpilot.Models.Requests;

namespace Schichtpilot.Validation;

public class GetAbsencesRequestValidator : AbstractValidator<GetAbsencesRequest>
{
    public GetAbsencesRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page has to be larger than 0");
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100)
            .WithMessage("Pagesize has to be between 1 and 100");
        RuleForEach(x => x.Status).IsEnumName(typeof(AbsenceStatusEnum))
            .When(x => x.Status != null && x.Status.Count > 0);
        RuleForEach(x => x.AbsenceType).IsEnumName(typeof(AbsenceTypeEnum))
            .When(x => x.AbsenceType != null && x.AbsenceType.Count > 0);
    }
}