using System.Threading;
using System.Threading.Tasks;

namespace Tripperist.Service.GooglePlaces;

/// <summary>
/// Wraps Google Places API calls so that higher level services can remain testable.
/// </summary>
public interface IGooglePlacesClient
{
    /// <summary>
    /// Attempts to resolve the Google Places identifier for the provided place name.
    /// </summary>
    /// <param name="placeName">Name of the place as retrieved from the Google Maps saved list.</param>
    /// <param name="cancellationToken">Token used to observe cancellation requests.</param>
    /// <returns>Lookup result when the place could be resolved, otherwise <c>null</c>.</returns>
    Task<PlaceLookupResult?> LookupAsync(string placeName, CancellationToken cancellationToken);
}
