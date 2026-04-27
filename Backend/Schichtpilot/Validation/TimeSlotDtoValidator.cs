using FluentValidation;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Validation;

/// <summary>
/// Validator for <see cref="TimeSlotDto"/>.
/// </summary>
public class TimeSlotDtoValidator : AbstractValidator<TimeSlotDto>
{
    public TimeSlotDtoValidator()
    {
        RuleFor(x => x.StartTime).NotNull().WithMessage("Start time required");
        RuleFor(x => x.EndTime).NotNull().WithMessage("Endtime required");
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).WithMessage("Endtime must be greater than start time");
        RuleFor(x => x.Breaks)
            .Must(HaveNoOverlappingSlots)
            .WithMessage("Breaks cannot overlap!");
        RuleForEach(x => x.Breaks).NotNull()
            .Must(x => x.StartTime < x.EndTime)
            .Must((dto, breakDto) => breakDto.StartTime >= dto.StartTime)
            .Must((dto, breakDto) => breakDto.EndTime <= dto.EndTime).WithMessage("Breaks cannot start or end before a timeslot");

    }

    private bool HaveNoOverlappingSlots(List<BreakDto> slots)
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