using FluentValidation;
using VividImagination.Modules.Moderation.Features.Reviews.Commands;

namespace VividImagination.Modules.Moderation.Features.Reviews.Commands;

public class UpdateModerationVerdictCommandValidator : AbstractValidator<UpdateModerationVerdictCommand>
{
    private static readonly string[] ValidVerdicts = ["Approved", "TaggedUnsafe", "Rejected"];

    public UpdateModerationVerdictCommandValidator()
    {
        RuleFor(x => x.PhotoId)
            .NotEmpty()
            .WithMessage("PhotoId is required.");

        RuleFor(x => x.Verdict)
            .NotEmpty()
            .Must(v => ValidVerdicts.Contains(v))
            .WithMessage("Verdict must be 'Approved', 'TaggedUnsafe', or 'Rejected'.");
    }
}
