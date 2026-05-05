using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

/// <summary>
/// Validator for <see cref="EditJobRoleDto"/>.
/// </summary>
public class EditJobRoleDtoValidator : AbstractValidator<EditJobRoleDto>
{
    public EditJobRoleDtoValidator()
    {
        RuleFor(dto => dto.Name).NotNull().NotEmpty().WithMessage("Name is required");
        RuleFor(dto => dto.Description).NotNull().NotEmpty().WithMessage("Description is required");
        RuleFor(dto => dto.Name).MinimumLength(2).MaximumLength(50).WithMessage("Name must be between 2 and 50 characters");
    }
}