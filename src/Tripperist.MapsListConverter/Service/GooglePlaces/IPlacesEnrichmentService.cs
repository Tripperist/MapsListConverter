using System.Threading;
using System.Threading.Tasks;
using Tripperist.Core.GoogleMaps;

namespace Tripperist.Service.GooglePlaces;

/// <summary>
/// Applies Google Places data to the scraped list entries.
/// </summary>
public interface IPlacesEnrichmentService
{
    /// <summary>
    /// Enriches the supplied list with Google Places information.
    /// </summary>
    /// <param name="list">List produced by the scraping pipeline.</param>
    /// <param name="cancellationToken">Token used to observe cancellation requests.</param>
    /// <returns>List with Google Places identifiers populated.</returns>
    Task<GoogleMapsList> EnrichAsync(GoogleMapsList list, CancellationToken cancellationToken);
}
