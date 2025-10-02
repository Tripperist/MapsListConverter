using System.Collections.Generic;

namespace Tripperist.MapsListConverter.Models;

/// <summary>
/// Represents the essential data returned by the Places Text Search API for a single result.
/// </summary>
/// <param name="PlaceId">The stable identifier assigned to the place.</param>
/// <param name="ResourceName">The resource name in the form <c>places/PLACE_ID</c>.</param>
/// <param name="Attributions">Provider attribution statements that accompany the result.</param>
public sealed record GooglePlaceSearchResult(string PlaceId, string ResourceName, IReadOnlyList<string>? Attributions);
