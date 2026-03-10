using FluentValidation;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Validation;

public class CreateAbsenceDtoValidator : AbstractValidator<CreateAbsenceDto>
{
    public CreateAbsenceDtoValidator()
    {
        RuleFor(x => x.StartDate).Must(x => x.Date >= DateTime.Now.Date);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
        RuleFor(x => x.AbsenceType).NotNull().NotEmpty().IsEnumName(typeof(AbsenceTypeEnum));
        RuleFor(x => x.Message).MinimumLength(3).MaximumLength(250).When(x => x.Message != null);
    }
}