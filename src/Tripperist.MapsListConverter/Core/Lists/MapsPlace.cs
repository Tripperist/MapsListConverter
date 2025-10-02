namespace Tripperist.MapsListConverter.Core.Lists;

/// <summary>
/// Represents a place inside a Google Maps saved list.
/// </summary>
public sealed class MapsPlace
{
    /// <summary>
    /// Gets or sets the display name for the place.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rating reported by Google Maps.
    /// </summary>
    public double? Rating { get; set; }

    /// <summary>
    /// Gets or sets the number of reviews associated with the place.
    /// </summary>
    public int? ReviewCount { get; set; }

    /// <summary>
    /// Gets or sets the note authored by the list creator.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Gets or sets the resolved Google Places API identifier.
    /// </summary>
    public string? PlaceId { get; set; }

    /// <summary>
    /// Gets or sets the image URL displayed in the saved list.
    /// </summary>
    public string? ImageUrl { get; set; }
}
