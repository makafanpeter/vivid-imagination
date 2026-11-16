using FluentValidation;
using FluentValidation.Results;

namespace VividImagination.Shared.Infrastructure.Validation;

public class NullReferenceAbstractValidator<T> : AbstractValidator<T>
{
    public override ValidationResult Validate(ValidationContext<T> context)
    {
        return context.InstanceToValidate == null
            ? new ValidationResult([new ValidationFailure("Request", "Request cannot be empty", "Error")])
            : base.Validate(context);
    }

    public override Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken cancellation = new CancellationToken())
    {
        return context.InstanceToValidate == null
            ? Task.FromResult(new ValidationResult([new ValidationFailure("Request", "Request cannot be empty", "Error")
            ]))
            : base.ValidateAsync(context, cancellation);
    }
}