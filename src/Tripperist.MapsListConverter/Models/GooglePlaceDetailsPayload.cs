using System.Collections.Generic;

namespace Tripperist.MapsListConverter.Models;

/// <summary>
/// Represents the data returned by the Places Details API when using the Essentials field mask.
/// </summary>
/// <param name="PlaceId">The stable identifier assigned to the place.</param>
/// <param name="ResourceName">The resource name in the form <c>places/PLACE_ID</c>.</param>
/// <param name="Attributions">Provider attribution statements associated with the details response.</param>
/// <param name="FormattedAddress">The formatted address string provided by Google.</param>
/// <param name="ShortFormattedAddress">The compact address representation.</param>
/// <param name="AddressDescriptor">Additional descriptive text for the address.</param>
/// <param name="AdrFormatAddress">The adr microformat representation of the address.</param>
/// <param name="Location">The place location coordinates.</param>
/// <param name="Viewport">The viewport that encloses the location.</param>
/// <param name="Types">The place types associated with the result.</param>
public sealed record GooglePlaceDetailsPayload(
    string PlaceId,
    string ResourceName,
    IReadOnlyList<string>? Attributions,
    string? FormattedAddress,
    string? ShortFormattedAddress,
    string? AddressDescriptor,
    string? AdrFormatAddress,
    GooglePlaceLocation? Location,
    GooglePlaceViewport? Viewport,
    IReadOnlyList<string>? Types);
