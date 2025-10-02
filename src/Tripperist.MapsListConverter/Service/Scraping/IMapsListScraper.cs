using Tripperist.MapsListConverter.Core.Lists;

namespace Tripperist.MapsListConverter.Service.Scraping;

/// <summary>
/// Loads Google Maps saved lists and materializes them into structured data.
/// </summary>
public interface IMapsListScraper
{
    /// <summary>
    /// Loads the saved list represented by <paramref name="listUri"/>.
    /// </summary>
    /// <param name="listUri">Target Google Maps URL.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>The hydrated <see cref="MapsList"/>.</returns>
    Task<MapsList> LoadAsync(Uri listUri, CancellationToken cancellationToken);
}
