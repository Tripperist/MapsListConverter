using System;
using System.Threading;
using System.Threading.Tasks;
using Tripperist.Core.GoogleMaps;

namespace Tripperist.Service.GoogleMaps;

/// <summary>
/// Defines the contract for retrieving Google Maps saved lists via browser automation.
/// </summary>
public interface IListScrapingService
{
    /// <summary>
    /// Loads the provided list URL, scrolls the page until all places are visible and returns the parsed representation.
    /// </summary>
    /// <param name="listUri">Public Google Maps list URI.</param>
    /// <param name="verboseLogging">When true additional diagnostic information is captured.</param>
    /// <param name="cancellationToken">Token used to observe cancellation requests.</param>
    /// <returns>Structured model representing the list.</returns>
    Task<GoogleMapsList> ScrapeAsync(Uri listUri, bool verboseLogging, CancellationToken cancellationToken);
}
