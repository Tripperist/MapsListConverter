using System.ComponentModel.DataAnnotations;

namespace Tripperist.Core.Configuration;

/// <summary>
/// Strongly typed representation of the Google Places configuration block.
/// </summary>
public sealed class GooglePlacesOptions
{
    /// <summary>
    /// Configuration section name used during binding.
    /// </summary>
    public const string SectionName = "GooglePlaces";

    /// <summary>
    /// API key used to authenticate requests against the Google Places API.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? ApiKey { get; init; }
}
