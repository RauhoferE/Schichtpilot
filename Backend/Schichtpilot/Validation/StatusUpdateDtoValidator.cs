using FluentValidation;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Validation;

/// <summary>
/// Validator for <see cref="StatusUpdateDto"/>.
/// </summary>
public class StatusUpdateDtoValidator : AbstractValidator<StatusUpdateDto>
{
    public StatusUpdateDtoValidator()
    {
        RuleFor(x => x.Status).NotNull().NotEmpty()
            .IsEnumName(typeof(AbsenceStatusEnum)).Must(x => x != nameof(AbsenceStatusEnum.Pending)).WithMessage("Status not found");
        RuleFor(x => x.ManagerMessage).MinimumLength(3).MaximumLength(250)
            .When(x => x.ManagerMessage != null).WithMessage("Maximum message length is 250 characters");
        RuleFor(x => x.ManagerMessage).NotEmpty().NotNull()
            .When(x => x.Status == nameof(AbsenceStatusEnum.Denied)).WithMessage("Denying an absence requires a message");
    }
}