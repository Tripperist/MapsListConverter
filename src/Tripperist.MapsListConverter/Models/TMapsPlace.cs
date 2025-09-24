namespace Tripperist.MapsListConverter.Models;

/// <summary>
/// Represents a place in a Tripperist Maps list, including enrichment metadata from Google Places.
/// </summary>
/// <param name="Name">Display name of the place.</param>
/// <param name="Address">Optional address provided by Google Maps for the place.</param>
/// <param name="Notes">Any notes authored by the list owner.</param>
/// <param name="Latitude">Latitude in decimal degrees when supplied by Google Maps.</param>
/// <param name="Longitude">Longitude in decimal degrees when supplied by Google Maps.</param>
/// <param name="GooglePlaceId">Place ID returned by the Google Places APIs.</param>
/// <param name="GooglePlaceResourceName">Resource name in the format <c>places/PLACE_ID</c>.</param>
public sealed record TMapsPlace(
    string Name,
    string? Address,
    string? Notes,
    double? Latitude,
    double? Longitude,
    string? GooglePlaceId = null,
    string? GooglePlaceResourceName = null);
