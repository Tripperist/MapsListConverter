namespace Tripperist.Core.GoogleMaps;

/// <summary>
/// Represents a single place that belongs to a Google Maps saved list.
/// </summary>
/// <param name="Name">Display name for the place.</param>
/// <param name="ImageUrl">Preview image presented in the list, when available.</param>
/// <param name="Rating">Average Google rating for the place.</param>
/// <param name="ReviewCount">Number of reviews contributing to the rating.</param>
/// <param name="Note">Free-form note added by the list author.</param>
/// <param name="PlaceId">Place identifier resolved through the Google Places API.</param>
/// <param name="Latitude">Latitude returned by the Places API.</param>
/// <param name="Longitude">Longitude returned by the Places API.</param>
/// <param name="Address">Formatted address returned by the Places API.</param>
public sealed record class GoogleMapsPlace(
    string Name,
    string? ImageUrl,
    double? Rating,
    int? ReviewCount,
    string? Note,
    string? PlaceId,
    double? Latitude,
    double? Longitude,
    string? Address);
