using FluentValidation;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Validation;

public class StatusUpdateDtoValidator : AbstractValidator<StatusUpdateDto>
{
    public StatusUpdateDtoValidator()
    {
        RuleFor(x => x.Status).NotNull().NotEmpty()
            .IsEnumName(typeof(AbsenceStatusEnum)).Must(x => x != nameof(AbsenceStatusEnum.Pending));
        RuleFor(x => x.ManagerMessage).MinimumLength(3).MaximumLength(250)
            .When(x => x.ManagerMessage != null);
        RuleFor(x => x.ManagerMessage).NotEmpty().NotNull()
            .When(x => x.Status == nameof(AbsenceStatusEnum.Denied));
    }
}