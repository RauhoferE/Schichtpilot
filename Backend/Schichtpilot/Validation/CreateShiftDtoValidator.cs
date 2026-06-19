using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

/// <summary>
/// Validator for <see cref="CreateShiftDto"/>.
/// </summary>
public class CreateShiftDtoValidator : AbstractValidator<CreateShiftDto>
{
    public CreateShiftDtoValidator(IValidator<ShiftRequirementDto> shiftDtoValidator, IValidator<TimeSlotDto> timeSlotDtoValidator)
    {
        RuleFor(x => x.Name).NotNull().NotEmpty()
            .MinimumLength(3).MaximumLength(25).WithMessage("Name is required and must be between 3 and 250 characters");
        RuleFor(x => x.ColorAsHex).NotNull().NotEmpty()
            .Matches("^#[0-9a-fA-F]{6}$").WithMessage("Color must be in a standard hex format");
        RuleFor(x => x.TimeSlots)
            .Must(x => x.DistinctBy(y => y.DayOfWeek).Count() == x.Count()).WithMessage("Every day can only have one timeslot");
        RuleForEach(x => x.TimeSlots).SetValidator(timeSlotDtoValidator);
        RuleFor(x => x.TimeSlots).Must(x => x.Count > 0).WithMessage("Atleast one Timeslot is needed!");
        RuleForEach(x => x.JobRequirements).NotNull()
            .Must(x => x.RequiredStaffCount > 0)
            .SetValidator(shiftDtoValidator);
        RuleFor(x => x.JobRequirements).NotNull()
            .Must(x => x.DistinctBy(y => y.JobId)
                .Count() == x.Count).WithMessage("Every job can only be added once to a shift");
    }
}