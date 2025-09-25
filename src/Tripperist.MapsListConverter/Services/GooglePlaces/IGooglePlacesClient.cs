using System.Threading;
using System.Threading.Tasks;
using Tripperist.MapsListConverter.Models;

namespace Tripperist.MapsListConverter.Services.GooglePlaces;

/// <summary>
/// Defines the Google Places API operations used by the application.
/// </summary>
public interface IGooglePlacesClient
{
    /// <summary>
    /// Executes a Places Text Search request using the supplied query.
    /// </summary>
    /// <param name="query">The textual query describing the place to locate.</param>
    /// <param name="cancellationToken">Token used to observe cancellation requests.</param>
    /// <returns>The first matching place from the search response, or <c>null</c> when the query could not be resolved.</returns>
    Task<GooglePlaceSearchResult?> SearchAsync(string query, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the details for the specified place identifier.
    /// </summary>
    /// <param name="placeId">The stable Google Place identifier returned by the text search.</param>
    /// <param name="cancellationToken">Token used to observe cancellation requests.</param>
    /// <returns>The place details when the identifier is recognised; otherwise <c>null</c>.</returns>
    Task<GooglePlaceDetailsPayload?> GetDetailsAsync(string placeId, CancellationToken cancellationToken);
}
