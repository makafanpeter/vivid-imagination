using FluentValidation;
using VividImagination.Modules.Identity.Features.Preferences.Commands;

namespace VividImagination.Modules.Identity.Features.Preferences.Validators;

public class UpdateSafeBrowsingPreferenceCommandValidator : AbstractValidator<UpdateSafeBrowsingPreferenceCommand>
{
    public UpdateSafeBrowsingPreferenceCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("User ID must be greater than zero.");

        RuleFor(x => x.Mode)
            .IsInEnum()
            .WithMessage("Invalid safe browsing mode specified.");
    }
}
