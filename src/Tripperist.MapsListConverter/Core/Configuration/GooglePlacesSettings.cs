using System.ComponentModel.DataAnnotations;
using Tripperist.MapsListConverter.App.Validation;

namespace Tripperist.MapsListConverter.Core.Configuration;

/// <summary>
/// Represents configuration required for interacting with the Google Places API.
/// </summary>
public sealed class GooglePlacesSettings
{
    /// <summary>
    /// Configuration section name used within configuration providers.
    /// </summary>
    public const string SectionName = "GooglePlaces";

    /// <summary>
    /// Gets or sets the API key used to authenticate requests against the Google Places API.
    /// </summary>
    [Required]
    [NotEmptyOrWhitespace]
    public string ApiKey { get; set; } = string.Empty;
}
