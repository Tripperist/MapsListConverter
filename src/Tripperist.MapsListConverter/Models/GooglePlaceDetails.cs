using System.Collections.Generic;

namespace Tripperist.MapsListConverter.Models;

/// <summary>
/// Represents metadata returned from the Google Places Text Search and Details APIs for a single place.
/// </summary>
public sealed class GooglePlaceDetails
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GooglePlaceDetails"/> class.
    /// </summary>
    /// <param name="placeId">The stable identifier assigned by Google to the place.</param>
    /// <param name="resourceName">The resource name in the form <c>places/PLACE_ID</c>.</param>
    /// <param name="attributions">Provider attributions that must be displayed alongside the data.</param>
    /// <param name="formattedAddress">The formatted address string returned by the Places API.</param>
    /// <param name="shortFormattedAddress">A shortened representation of the address when available.</param>
    /// <param name="addressDescriptor">Descriptive text for the address (for example a landmark).</param>
    /// <param name="adrFormatAddress">The <c>adr</c> microformat representation of the address.</param>
    /// <param name="location">The latitude and longitude of the place if available.</param>
    /// <param name="viewport">The viewport that fully contains the location.</param>
    /// <param name="types">The place types associated with the result.</param>
    public GooglePlaceDetails(
        string placeId,
        string resourceName,
        IReadOnlyList<string>? attributions,
        string? formattedAddress,
        string? shortFormattedAddress,
        string? addressDescriptor,
        string? adrFormatAddress,
        GooglePlaceLocation? location,
        GooglePlaceViewport? viewport,
        IReadOnlyList<string>? types)
    {
        PlaceId = placeId;
        ResourceName = resourceName;
        Attributions = attributions;
        FormattedAddress = formattedAddress;
        ShortFormattedAddress = shortFormattedAddress;
        AddressDescriptor = addressDescriptor;
        AdrFormatAddress = adrFormatAddress;
        Location = location;
        Viewport = viewport;
        Types = types;
    }

    /// <summary>
    /// Gets the stable identifier assigned by Google to the place.
    /// </summary>
    public string PlaceId { get; }

    /// <summary>
    /// Gets the resource name in the form <c>places/PLACE_ID</c>.
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// Gets provider attributions that must be displayed alongside the data.
    /// </summary>
    public IReadOnlyList<string>? Attributions { get; }

    /// <summary>
    /// Gets the formatted address string returned by the Places API.
    /// </summary>
    public string? FormattedAddress { get; }

    /// <summary>
    /// Gets a shortened representation of the address when available.
    /// </summary>
    public string? ShortFormattedAddress { get; }

    /// <summary>
    /// Gets descriptive text for the address (for example a landmark).
    /// </summary>
    public string? AddressDescriptor { get; }

    /// <summary>
    /// Gets the adr microformat representation of the address.
    /// </summary>
    public string? AdrFormatAddress { get; }

    /// <summary>
    /// Gets the latitude and longitude of the place if available.
    /// </summary>
    public GooglePlaceLocation? Location { get; }

    /// <summary>
    /// Gets the viewport that fully contains the location.
    /// </summary>
    public GooglePlaceViewport? Viewport { get; }

    /// <summary>
    /// Gets the place types associated with the result.
    /// </summary>
    public IReadOnlyList<string>? Types { get; }
}

/// <summary>
/// Represents a latitude and longitude coordinate returned by the Places API.
/// </summary>
/// <param name="Latitude">Latitude in decimal degrees.</param>
/// <param name="Longitude">Longitude in decimal degrees.</param>
public sealed record GooglePlaceLocation(double Latitude, double Longitude);

/// <summary>
/// Represents the viewport that bounds a place returned by the Places API.
/// </summary>
/// <param name="Low">The south-west corner of the viewport.</param>
/// <param name="High">The north-east corner of the viewport.</param>
public sealed record GooglePlaceViewport(GooglePlaceLocation Low, GooglePlaceLocation High);
