using System;
using System.Collections.Generic;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tripperist.Core.GoogleMaps;
using Tripperist.Core.Localization;

namespace Tripperist.Service.GooglePlaces;

/// <summary>
/// Enriches the scraped list with data retrieved from the Google Places API.
/// </summary>
public sealed class PlacesEnrichmentService(IGooglePlacesClient placesClient, ILogger<PlacesEnrichmentService> logger) : IPlacesEnrichmentService
{
    private readonly IGooglePlacesClient _placesClient = placesClient ?? throw new ArgumentNullException(nameof(placesClient));
    private readonly ILogger<PlacesEnrichmentService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ResourceManager _logMessages = ResourceCatalog.LogMessages;

    /// <inheritdoc />
    public async Task<GoogleMapsList> EnrichAsync(GoogleMapsList list, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(list);
        cancellationToken.ThrowIfCancellationRequested();

        var enrichedPlaces = new List<GoogleMapsPlace>(list.Places.Count);
        foreach (var place in list.Places)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogDebug(_logMessages.GetString("PlacesEnrichmentStarting"), place.Name);

            var lookup = await _placesClient.LookupAsync(place.Name, cancellationToken).ConfigureAwait(false);
            if (lookup is null)
            {
                enrichedPlaces.Add(place);
                continue;
            }

            enrichedPlaces.Add(place with
            {
                PlaceId = lookup.PlaceId,
                Latitude = lookup.Latitude,
                Longitude = lookup.Longitude,
                Address = lookup.Address
            });
        }

        _logger.LogInformation(_logMessages.GetString("PlacesEnrichmentCompleted"), enrichedPlaces.Count);
        return list with { Places = enrichedPlaces };
    }
}
