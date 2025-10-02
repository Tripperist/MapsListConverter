using System.Resources;

namespace Tripperist.Core.Localization;

/// <summary>
/// Centralizes access to the application's localized resources.
/// </summary>
public static class ResourceCatalog
{
    /// <summary>
    /// Provides localized log message templates.
    /// </summary>
    public static ResourceManager LogMessages { get; } = new("Tripperist.MapsListConverter.Resources.LogMessages", typeof(ResourceCatalog).Assembly);

    /// <summary>
    /// Provides localized error message templates.
    /// </summary>
    public static ResourceManager ErrorMessages { get; } = new("Tripperist.MapsListConverter.Resources.ErrorMessages", typeof(ResourceCatalog).Assembly);
}
