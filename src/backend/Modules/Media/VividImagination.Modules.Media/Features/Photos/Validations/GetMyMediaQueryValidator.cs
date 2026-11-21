using FluentValidation;
using VividImagination.Modules.Media.Features.Photos.Queries;

namespace VividImagination.Modules.Media.Features.Photos.Validations;

public class GetMyMediaQueryValidator : AbstractValidator<GetMyMediaQuery>
{
    public GetMyMediaQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");
    }
}
