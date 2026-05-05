using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

/// <summary>
/// Provides validation rules for <see cref="AddressDto"/>.
/// </summary>
public class AddressDtoValidator : AbstractValidator<AddressDto>
{
    public AddressDtoValidator()
    {
        RuleFor(x => x.PostalCode).GreaterThanOrEqualTo(1000).LessThanOrEqualTo(9999).WithMessage("Postal code must be between 1000 and 9999");
        RuleFor(x => x.City).NotEmpty().WithMessage("City is required");
        RuleFor(x => x.City).MinimumLength(2).MaximumLength(20).WithMessage("City is required");
        RuleFor(x => x.Street).NotEmpty().WithMessage("Street is required");
        RuleFor(x => x.Street).MinimumLength(2).MaximumLength(50).WithMessage("Street is required");
    }
}