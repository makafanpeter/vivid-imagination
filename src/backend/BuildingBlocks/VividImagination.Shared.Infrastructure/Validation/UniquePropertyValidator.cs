using FluentValidation;
using FluentValidation.Validators;

namespace VividImagination.Shared.Infrastructure.Validation;

public class UniquePropertyValidator<T, TProperty>(Func<TProperty, string?> propertySelector, string propertyName)
    : PropertyValidator<T, List<TProperty>?>
{
    public override bool IsValid(ValidationContext<T> context, List<TProperty>? value)
    {
        if (value == null || value.Count == 0)
        {
            return true;
        }

        var nonUniqueKeys = value
            .Where(x => !string.IsNullOrWhiteSpace(propertySelector(x)))
            .GroupBy(x => propertySelector(x)!.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (nonUniqueKeys.Count > 0)
        {
            string failedItems = string.Join(", ", nonUniqueKeys);
            context.MessageFormatter.AppendArgument("propertyName", propertyName);
            context.MessageFormatter.AppendArgument("symbols", failedItems);
            return false;
        }

        return true;
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "The following {propertyName}(s) already exist: {symbols}";
    }

    public override string Name => "UniquePropertyValidator";
}