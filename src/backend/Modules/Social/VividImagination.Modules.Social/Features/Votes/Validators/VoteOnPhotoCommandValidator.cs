using FluentValidation;
using VividImagination.Modules.Social.Features.Votes.Commands;
using VividImagination.Shared.Infrastructure.Validation;

namespace VividImagination.Modules.Social.Features.Votes.Validators;

public class VoteOnPhotoCommandValidator : NullReferenceAbstractValidator<VoteOnPhotoCommand>
{
    public VoteOnPhotoCommandValidator()
    {
        RuleFor(x => x.PhotoId)
            .NotEmpty()
            .WithMessage("PhotoId is required.");

        RuleFor(x => x.Value)
            .Must(v => v is 1 or -1 or 0)
            .WithMessage("Vote value must be +1 (Upvote), -1 (Downvote), or 0 (Cancel).");
    }
}
