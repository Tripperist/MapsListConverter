using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Tripperist.MapsListConverter.Models;
using Microsoft.Extensions.Logging;

namespace Tripperist.MapsListConverter.Services;

/// <summary>
/// Retrieves a Tripperist Maps list page and translates it into strongly typed models using HTTP requests.
/// </summary>
public sealed class TMapsListScraper : IMapsListScraper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TMapsListScraper> _logger;

    public TMapsListScraper(HttpClient httpClient, ILogger<TMapsListScraper> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Downloads the Google Maps list and extracts its metadata and places.
    /// </summary>
    public async Task<TMapsListData> FetchListAsync(Uri listUri, CancellationToken cancellationToken = default)
    {
        if (listUri is null)
        {
            throw new ArgumentNullException(nameof(listUri));
        }

        _logger.LogInformation("Downloading Google Maps list from {Uri}", listUri);

        using var response = await _httpClient
            .GetAsync(listUri, HttpCompletionOption.ResponseContentRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var listData = TMapsListParser.ParseFromHtml(htmlContent, _logger);
        _logger.LogInformation("Extracted {Count} places from list '{ListName}'.", listData.Places.Count, listData.Name);
        return listData;
    }
}
