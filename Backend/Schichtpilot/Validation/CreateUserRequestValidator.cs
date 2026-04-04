using FluentValidation;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Requests;

namespace Schichtpilot.Validation;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator(IValidator<AddressDto> addressDtoValidator)
    {
        RuleFor(x => x.Email).NotNull().NotEmpty().WithMessage("Email is required");
        RuleFor(x => x.Email).EmailAddress().WithMessage("Invalid email");
        RuleFor(x => x.Password).NotNull().NotEmpty().WithMessage("Password is required");
        RuleFor(x => x.Password).Matches("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*#?&])[A-Za-z\\d@$!%*#?&]{12,}$").WithMessage("Password needs to be atleast 12 characters long and contain 1 uppercase, 1 lowercase, 1 number and 1 special character");
        RuleFor(x => x.Birthdate).NotNull().WithMessage("Birthdate is required");
        RuleFor(x => x.Birthdate).Must(x => x.Date < DateTime.Now).WithMessage("Birthdate must be in the past");
        RuleFor(x => x.FirstName).NotNull().NotEmpty().WithMessage("First name is required");
        RuleFor(x => x.LastName).NotNull().NotEmpty().WithMessage("Last name is required");
        RuleFor(x => x.FirstName).MinimumLength(3).MaximumLength(20).WithMessage("First name must be between 3 and 20 characters");
        RuleFor(x => x.LastName).MinimumLength(3).MaximumLength(20).WithMessage("Last name must be between 3 and 20 characters");
        RuleFor(x => x.AddressDto).NotNull().WithMessage("Address is required");
        RuleFor(x => x.AddressDto).SetValidator(addressDtoValidator);
    }
    
}