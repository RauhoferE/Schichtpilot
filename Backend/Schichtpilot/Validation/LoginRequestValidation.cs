using FluentValidation;
using Schichtpilot.Models.Requests;

namespace Schichtpilot.Validation;

/// <summary>
/// Validator for <see cref="LoginRequest"/>.
/// </summary>
public class LoginRequestValidation : AbstractValidator<LoginRequest>
{
    public LoginRequestValidation()
    {
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress().WithMessage("Email is required");
        RuleFor(x => x.Password).NotNull().NotEmpty().WithMessage("Password is required");
    }
}