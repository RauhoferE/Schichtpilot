using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

public class CreateShiftDtoValidator : AbstractValidator<CreateShiftDto>
{
    public CreateShiftDtoValidator(IValidator<ShiftRequirementDto> shiftDtoValidator, IValidator<TimeSlotDto> timeSlotDtoValidator)
    {
        RuleFor(x => x.Name).NotNull().NotEmpty()
            .MinimumLength(3).MaximumLength(25);
        RuleFor(x => x.ColorAsHex).NotNull().NotEmpty()
            .Matches("^#[0-9a-fA-F]{6}$");
        RuleFor(x => x.TimeSlots)
            .Must(HaveNoOverlappingSlots)
            .Must(x => x.DistinctBy(y => y.DayOfWeek).Count() == x.Count());
        RuleForEach(x => x.TimeSlots).SetValidator(timeSlotDtoValidator);
        RuleForEach(x => x.JobRequirements).NotNull()
            .Must(x => x.RequiredStaffCount > 0)
            .SetValidator(shiftDtoValidator);
        RuleFor(x => x.JobRequirements).NotNull()
            .Must(x => x.DistinctBy(y => y.JobId)
                .Count() == x.Count);
    }

    private bool HaveNoOverlappingSlots(List<TimeSlotDto> slots)
    {
        if (slots == null || slots.Count <= 1) return true;

        // Sort by StartTime to make the comparison O(n log n) instead of O(n^2)
        var sortedSlots = slots.OrderBy(s => s.StartTime).ToList();

        for (int i = 0; i < sortedSlots.Count - 1; i++)
        {
            var current = sortedSlots[i];
            var next = sortedSlots[i + 1];

            // If the next slot starts before the current one ends, they overlap
            if (next.StartTime < current.EndTime)
            {
                return false;
            }
        }

        return true;
    }
}