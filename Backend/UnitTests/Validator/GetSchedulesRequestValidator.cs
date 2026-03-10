using FluentValidation;
using Schichtpilot.Models.Enums;
using Schichtpilot.Models.Requests;

namespace UnitTests.Validator;

public class GetSchedulesRequestValidator : AbstractValidator<GetSchedulesRequest>
{
    public GetSchedulesRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page has to be larger than 0");
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100)
            .WithMessage("Pagesize has to be between 1 and 100");
        RuleFor(x => x.Status).NotEmpty().NotNull()
            .IsEnumName(typeof(ScheduleStatusEnum));
    }
}