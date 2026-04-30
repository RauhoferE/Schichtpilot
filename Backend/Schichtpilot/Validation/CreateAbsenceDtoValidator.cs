using FluentValidation;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Validation;

/// <summary>
/// Validator for <see cref="CreateAbsenceDto"/>.
/// </summary>
public class CreateAbsenceDtoValidator : AbstractValidator<CreateAbsenceDto>
{
    public CreateAbsenceDtoValidator()
    {
        RuleFor(x => x.StartDate).Must(x => x.Date >= DateTime.Now.Date).WithMessage("Start date must be in the future");
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("End date must be later than the start date");
        RuleFor(x => x.AbsenceType).NotNull().NotEmpty().IsEnumName(typeof(AbsenceTypeEnum)).WithMessage("Absence type is required");
        RuleFor(x => x.Message).MinimumLength(3).MaximumLength(250).When(x => x.Message != null).WithMessage("The absence message must be between 3 and 250 characters");
    }
}