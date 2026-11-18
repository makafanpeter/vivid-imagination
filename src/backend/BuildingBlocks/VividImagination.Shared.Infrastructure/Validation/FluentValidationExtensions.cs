using FluentValidation;

namespace VividImagination.Shared.Infrastructure.Validation;

public static class FluentValidationExtensions
{
    public static IRuleBuilderOptions<T, string?> MustBeValidEnum<T, TEnum>(
        this IRuleBuilder<T, string?> ruleBuilder) 
        where TEnum : struct, Enum
    {
        var allowedValues = string.Join(", ", Enum.GetNames<TEnum>());
        
        return ruleBuilder
            .NotEmpty()
            .IsEnumName(typeof(TEnum))
            .WithMessage($"'{{PropertyName}}' has a range of values which does not include '{{PropertyValue}}'. Allowed values are: {allowedValues}");
    }
}