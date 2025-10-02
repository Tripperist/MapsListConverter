using System.ComponentModel.DataAnnotations;

namespace Tripperist.MapsListConverter.App.Validation;

/// <summary>
/// Ensures a string option contains non-whitespace characters. This avoids subtle misconfiguration issues
/// when an environment variable exists but lacks a usable value.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class NotEmptyOrWhitespaceAttribute : ValidationAttribute
{
    /// <inheritdoc />
    public override bool IsValid(object? value)
    {
        return value switch
        {
            null => true,
            string text => !string.IsNullOrWhiteSpace(text),
            _ => false
        };
    }
}
