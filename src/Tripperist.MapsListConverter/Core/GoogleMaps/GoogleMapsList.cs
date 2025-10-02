using System.Collections.Generic;

namespace Tripperist.Core.GoogleMaps;

/// <summary>
/// Represents the metadata captured for a single Google Maps saved list.
/// </summary>
/// <param name="Name">Display name of the list shown in Google Maps.</param>
/// <param name="Description">Optional free-form description authored by the list creator.</param>
/// <param name="Creator">Display name of the user who shared the list publicly.</param>
/// <param name="Places">Collection of all places that were discovered after scrolling the entire list.</param>
public sealed record class GoogleMapsList(
    string Name,
    string? Description,
    string? Creator,
    IReadOnlyList<GoogleMapsPlace> Places);
