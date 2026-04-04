using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

public class CreateJobRoleDtoValidator : AbstractValidator<CreateJobRoleDto>
{
    public CreateJobRoleDtoValidator()
    {
        RuleFor(dto => dto.Name).NotNull().NotEmpty().WithMessage("Name is required");
        RuleFor(dto => dto.Description).NotNull().NotEmpty().WithMessage("Description is required");
        RuleFor(dto => dto.Name).MinimumLength(2).MaximumLength(50).WithMessage("Name must be between 2 and 50 characters");
        RuleFor(dto => dto.Description).MinimumLength(2).MaximumLength(250).WithMessage("Description must be between 2 and 50 characters");
        RuleFor(dto => dto.DependentOnJobRoleIds).Must(x => x.Distinct().Count() == x.Count).WithMessage("Multiples of the same job role ids");
    }
}