using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

public class AddressDtoValidator : AbstractValidator<AddressDto>
{
    public AddressDtoValidator()
    {
        RuleFor(x => x.PostalCode).GreaterThanOrEqualTo(1000).LessThanOrEqualTo(9999).WithMessage("Postal code must be between 1000 and 9999");
        RuleFor(x => x.City).NotEmpty().WithMessage("City is required");
        RuleFor(x => x.City).MinimumLength(2).WithMessage("City is required");
        RuleFor(x => x.Street).NotEmpty().WithMessage("Street is required");
        RuleFor(x => x.Street).MinimumLength(2).WithMessage("Street is required");
    }
}