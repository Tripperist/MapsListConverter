namespace Tripperist.MapsListConverter.App.Validation;

/// <summary>
/// Contract for validating command options.
/// </summary>
/// <typeparam name="TOptions">Option type to validate.</typeparam>
public interface IOptionsValidator<in TOptions>
{
    /// <summary>
    /// Validates the supplied options instance.
    /// </summary>
    /// <param name="options">Options to validate.</param>
    /// <returns>A collection of validation error messages. Empty when validation succeeds.</returns>
    IReadOnlyCollection<string> Validate(TOptions options);
}
