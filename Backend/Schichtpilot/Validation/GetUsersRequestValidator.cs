using FluentValidation;
using Schichtpilot.Models.Enums;
using Schichtpilot.Models.Requests;

namespace Schichtpilot.Validation;

public class GetUsersRequestValidator : AbstractValidator<GetUsersRequest>
{
    public GetUsersRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page has to be larger than 0");
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100)
            .WithMessage("Pagesize has to be between 1 and 100");
        RuleFor(x => x.SortProperty).NotNull().NotEmpty().IsEnumName(typeof(UserSortEnum));
        RuleFor(x => x.AccountStatus).NotNull().NotEmpty().IsEnumName(typeof(AccountStatusEnum));
    }
}