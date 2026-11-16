using FluentValidation;
using FluentValidation.Validators;

namespace VividImagination.Shared.Infrastructure.Validation;

public class UniqueElementsValidator<T, TElement>(string elementName = "element")
    : PropertyValidator<T, List<TElement>?>
{
    public override bool IsValid(ValidationContext<T> context, List<TElement>? value)
    {
        if (value == null || !value.Any())
            return true;

        var duplicates = value
            .GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            string duplicateItems = string.Join(", ", duplicates);
            context.MessageFormatter.AppendArgument("elementName", elementName);
            context.MessageFormatter.AppendArgument("duplicates", duplicateItems);
            return false;
        }

        return true;
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "Duplicate {elementName}(s) found: {duplicates}";
    }

    public override string Name => "UniqueElementsValidator";
}