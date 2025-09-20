using System;
using System.Threading;
using System.Threading.Tasks;
using Tripperist.MapsListConverter.Models;

namespace Tripperist.MapsListConverter.Services;

/// <summary>
/// Contract implemented by services capable of downloading and translating Google Maps list data.
/// </summary>
public interface IMapsListScraper
{
    /// <summary>
    /// Fetches the list metadata and places contained in the supplied public Google Maps list URL.
    /// </summary>
    Task<TMapsListData> FetchListAsync(Uri listUri, CancellationToken cancellationToken = default);
}
