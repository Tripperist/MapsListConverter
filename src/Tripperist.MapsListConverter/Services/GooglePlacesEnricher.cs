using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tripperist.MapsListConverter.Models;

namespace Tripperist.MapsListConverter.Services;

/// <summary>
/// Enhances Tripperist map entries with authoritative data from the Google Places APIs.
/// </summary>
public sealed class GooglePlacesEnricher(IGooglePlacesClient placesClient, ILogger<GooglePlacesEnricher> logger)
{
    private readonly IGooglePlacesClient _placesClient = placesClient ?? throw new ArgumentNullException(nameof(placesClient));
    private readonly ILogger<GooglePlacesEnricher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Enriches every place entry with official Google Places identifiers and geometry when available.
    /// </summary>
    /// <param name="listData">The Tripperist list returned from the scraper.</param>
    /// <param name="cancellationToken">Token used to cancel the enrichment.</param>
    /// <returns>An updated list that includes Google Places information.</returns>
    public async Task<TMapsListData> EnrichAsync(TMapsListData listData, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(listData);

        if (listData.Places.Count == 0)
        {
            _logger.LogInformation("Skipping Google Places enrichment because the list contains no places.");
            return listData;
        }

        var enrichedPlaces = new List<TMapsPlace>(listData.Places.Count);

        foreach (var place in listData.Places)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Enriching place '{PlaceName}'.", place.Name);

            var searchResult = await _placesClient.SearchAsync(place, cancellationToken).ConfigureAwait(false);
            if (searchResult is null)
            {
                enrichedPlaces.Add(place);
                continue;
            }

            var details = await _placesClient.GetDetailsAsync(searchResult.PlaceId, cancellationToken).ConfigureAwait(false);

            var updatedPlace = place with
            {
                GooglePlaceId = searchResult.PlaceId,
                GooglePlaceResourceName = searchResult.ResourceName,
                Address = details?.FormattedAddress ?? place.Address,
                Latitude = details?.Latitude ?? place.Latitude,
                Longitude = details?.Longitude ?? place.Longitude
            };

            if (details is null)
            {
                _logger.LogWarning("Google Place details were unavailable for {PlaceId}.", searchResult.PlaceId);
            }

            enrichedPlaces.Add(updatedPlace);
        }

        return listData with { Places = enrichedPlaces };
    }
}
