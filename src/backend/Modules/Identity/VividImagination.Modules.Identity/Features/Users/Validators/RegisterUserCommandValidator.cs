using FluentValidation;
using VividImagination.Modules.Identity.Features.Users.Commands;

namespace VividImagination.Modules.Identity.Features.Users.Validators;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.FirstName)
            .MaximumLength(150).WithMessage("First name must not exceed 150 characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(150).WithMessage("Last name must not exceed 150 characters.");

        When(x => x.SafeBrowsingMode.HasValue, () =>
        {
            RuleFor(x => x.SafeBrowsingMode!.Value)
                .IsInEnum().WithMessage("Invalid safe browsing mode specified.");
        });
    }
}
