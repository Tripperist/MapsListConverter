using System.Threading;
using System.Threading.Tasks;
using Tripperist.MapsListConverter.Models;

namespace Tripperist.MapsListConverter.Services;

/// <summary>
/// Contract for components capable of retrieving Google Places data for Tripperist entries.
/// </summary>
public interface IGooglePlacesClient
{
    /// <summary>
    /// Executes a Google Places Text Search using the supplied place information.
    /// </summary>
    /// <param name="place">Place extracted from the Tripperist list.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The matching place identifiers when successful; otherwise, <c>null</c>.</returns>
    Task<GooglePlaceSearchResult?> SearchAsync(TMapsPlace place, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves details for a specific place identifier.
    /// </summary>
    /// <param name="placeId">The Google Place ID returned from Text Search.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The detailed place information when available; otherwise, <c>null</c>.</returns>
    Task<GooglePlaceDetails?> GetDetailsAsync(string placeId, CancellationToken cancellationToken);
}

/// <summary>
/// Holds identifier data returned from the Google Places Text Search API.
/// </summary>
/// <param name="PlaceId">Stable Google Place ID suitable for Place Details requests.</param>
/// <param name="ResourceName">The resource name in the form <c>places/PLACE_ID</c>.</param>
public sealed record GooglePlaceSearchResult(string PlaceId, string ResourceName);

/// <summary>
/// Represents the subset of Place Details fields retrieved under the Essentials SKU.
/// </summary>
/// <param name="FormattedAddress">The standardized address returned by Google.</param>
/// <param name="Latitude">Latitude value returned from the <c>location</c> field.</param>
/// <param name="Longitude">Longitude value returned from the <c>location</c> field.</param>
public sealed record GooglePlaceDetails(string? FormattedAddress, double? Latitude, double? Longitude);
