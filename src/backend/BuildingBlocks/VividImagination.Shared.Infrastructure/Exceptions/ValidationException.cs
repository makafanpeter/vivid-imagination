using FluentValidation.Results;

namespace VividImagination.Shared.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when validation errors occur in the application.
/// </summary>
public class ValidationException : AppException
{
    private const string DefaultValidationMessage = "One or more validation failures have occurred.";
    private const string DefaultValidationCode = "Invalid Request";
    private const string SingleValidationMessage = "Validation failed.";

    /// <summary>
    /// Gets or sets the validation failures, grouped by property name.
    /// </summary>
    public IDictionary<string, string[]> Failures { get; set; } = new Dictionary<string, string[]>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with default message.
    /// </summary>
    public ValidationException()
        : base(DefaultValidationMessage, DefaultValidationCode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ValidationException(string message)
        : base(message, DefaultValidationCode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with FluentValidation failures.
    /// </summary>
    /// <param name="failures">The list of validation failures.</param>
    public ValidationException(List<ValidationFailure> failures)
        : this()
    {
        var failureGroups = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage);

        foreach (var failureGroup in failureGroups)
        {
            var propertyName = failureGroup.Key;
            var propertyFailures = failureGroup.ToArray();
            Failures.Add(propertyName, propertyFailures);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a dictionary of errors.
    /// </summary>
    /// <param name="errors">Dictionary containing property names and their associated error messages.</param>
    public ValidationException(IDictionary<string, string[]>? errors)
        : this(DefaultValidationMessage)
    {
        Failures = errors ?? new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a single property validation error.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="errorMessage">The error message.</param>
    public ValidationException(string propertyName, string errorMessage)
        : this(SingleValidationMessage)
    {
        Failures = new Dictionary<string, string[]>
        {
            { propertyName, [errorMessage] }
        };
    }


    public override string ToString()
    {
        if (!Failures.Any())
            return base.ToString();

        var errors = string.Join(", ", Failures.Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}"));

        return $"{base.ToString()} - Validation Failures: {errors}";
    }
}