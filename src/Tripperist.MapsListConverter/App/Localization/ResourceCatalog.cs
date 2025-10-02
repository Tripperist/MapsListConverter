using System.Globalization;
using System.Resources;

namespace Tripperist.MapsListConverter.App.Localization;

/// <summary>
/// Centralizes access to localized log and error messages. Using <see cref="ResourceManager"/> keeps user-facing
/// text out of the code paths and simplifies future localization efforts.
/// </summary>
public sealed class ResourceCatalog
{
    private readonly ResourceManager _logMessages;
    private readonly ResourceManager _errorMessages;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceCatalog"/> class.
    /// </summary>
    public ResourceCatalog()
    {
        _logMessages = new ResourceManager("Tripperist.MapsListConverter.Resources.LogMessages", typeof(ResourceCatalog).Assembly);
        _errorMessages = new ResourceManager("Tripperist.MapsListConverter.Resources.ErrorMessages", typeof(ResourceCatalog).Assembly);
    }

    /// <summary>
    /// Retrieves a localized log message for the specified key.
    /// </summary>
    /// <param name="key">Resource key representing the log message.</param>
    /// <param name="culture">Culture used for localization.</param>
    /// <returns>Localized message text.</returns>
    public string Log(string key, CultureInfo culture) =>
        _logMessages.GetString(key, culture) ?? key;

    /// <summary>
    /// Retrieves a localized error message for the specified key.
    /// </summary>
    /// <param name="key">Resource key representing the error message.</param>
    /// <param name="culture">Culture used for localization.</param>
    /// <returns>Localized error text.</returns>
    public string Error(string key, CultureInfo culture) =>
        _errorMessages.GetString(key, culture) ?? key;
}
