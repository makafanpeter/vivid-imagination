using FluentValidation;
using VividImagination.Modules.Social.Features.Reports.Commands;
using VividImagination.Shared.Infrastructure.Validation;

namespace VividImagination.Modules.Social.Features.Reports.Validators;

public class ReportPhotoCommandValidator : NullReferenceAbstractValidator<ReportPhotoCommand>
{
    public ReportPhotoCommandValidator()
    {
        RuleFor(x => x.PhotoId)
            .NotEmpty()
            .WithMessage("PhotoId is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters.");
    }
}
