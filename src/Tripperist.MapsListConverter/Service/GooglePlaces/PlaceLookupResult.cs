namespace Tripperist.Service.GooglePlaces;

/// <summary>
/// Represents the response information required to enrich a Google Maps place entry.
/// </summary>
/// <param name="PlaceId">Unique identifier assigned by Google Places.</param>
/// <param name="Latitude">Latitude derived from the Places API.</param>
/// <param name="Longitude">Longitude derived from the Places API.</param>
/// <param name="Address">Formatted address returned by the Places API.</param>
public sealed record class PlaceLookupResult(string PlaceId, double? Latitude, double? Longitude, string? Address);
