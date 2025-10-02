using System.ComponentModel.DataAnnotations;

namespace Tripperist.MapsListConverter.App.Validation;

/// <summary>
/// Validates options using <see cref="ValidationAttribute"/> metadata. Centralizing validation keeps command
/// handlers focused on orchestration logic instead of repetitive guard clauses.
/// </summary>
/// <typeparam name="TOptions">Type being validated.</typeparam>
public sealed class DataAnnotationsValidator<TOptions> : IOptionsValidator<TOptions>
{
    /// <inheritdoc />
    public IReadOnlyCollection<string> Validate(TOptions options)
    {
        if (options is null)
        {
            return new[] { "Options cannot be null." };
        }

        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);
        if (isValid)
        {
            return Array.Empty<string>();
        }

        return results.Select(result => result.ErrorMessage ?? string.Empty).Where(message => !string.IsNullOrWhiteSpace(message)).ToArray();
    }
}
