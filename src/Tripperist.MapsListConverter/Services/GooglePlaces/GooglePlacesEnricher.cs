using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tripperist.MapsListConverter.Models;

namespace Tripperist.MapsListConverter.Services.GooglePlaces;

/// <summary>
/// Enriches Tripperist place data using the Google Places Text Search and Details APIs.
/// </summary>
public sealed class GooglePlacesEnricher
{
    private readonly IGooglePlacesClient _client;
    private readonly ILogger<GooglePlacesEnricher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GooglePlacesEnricher"/> class.
    /// </summary>
    /// <param name="client">The Places API client used to issue requests.</param>
    /// <param name="logger">Logger used to capture diagnostic information.</param>
    public GooglePlacesEnricher(IGooglePlacesClient client, ILogger<GooglePlacesEnricher> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Enriches each place in the list with Google Places metadata when available.
    /// </summary>
    /// <param name="listData">The Tripperist list data to enrich.</param>
    /// <param name="cancellationToken">Token used to observe cancellation requests.</param>
    /// <returns>An updated list where each place contains Google Places metadata when it could be resolved.</returns>
    public async Task<TMapsListData> EnrichAsync(TMapsListData listData, CancellationToken cancellationToken)
    {
        if (listData is null)
        {
            throw new ArgumentNullException(nameof(listData));
        }

        if (listData.Places.Count == 0)
        {
            return listData;
        }

        var enrichedPlaces = new List<TMapsPlace>(listData.Places.Count);

        foreach (var place in listData.Places)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = BuildQuery(place);

            if (string.IsNullOrWhiteSpace(query))
            {
                enrichedPlaces.Add(place);
                continue;
            }

            try
            {
                var searchResult = await _client.SearchAsync(query, cancellationToken).ConfigureAwait(false);
                if (searchResult is null)
                {
                    enrichedPlaces.Add(place);
                    continue;
                }

                var details = await _client.GetDetailsAsync(searchResult.PlaceId, cancellationToken).ConfigureAwait(false);
                var combinedDetails = CreateGoogleDetails(searchResult, details);

                var address = string.IsNullOrWhiteSpace(place.Address)
                    ? combinedDetails?.FormattedAddress ?? place.Address
                    : place.Address;

                var latitude = place.Latitude ?? combinedDetails?.Location?.Latitude;
                var longitude = place.Longitude ?? combinedDetails?.Location?.Longitude;

                enrichedPlaces.Add(new TMapsPlace(place.Name, address, place.Notes, latitude, longitude, combinedDetails));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enrich place '{PlaceName}' using Google Places.", place.Name);
                enrichedPlaces.Add(place);
            }
        }

        return new TMapsListData(listData.Name, listData.Description, listData.Creator, enrichedPlaces);
    }

    private static string? BuildQuery(TMapsPlace place)
    {
        if (place is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(place.Name))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(place.Address)
            ? place.Name
            : $"{place.Name} {place.Address}";
    }

    private static GooglePlaceDetails? CreateGoogleDetails(
        GooglePlaceSearchResult searchResult,
        GooglePlaceDetailsPayload? detailsPayload)
    {
        var attributions = CombineAttributions(searchResult.Attributions, detailsPayload?.Attributions);

        if (detailsPayload is null)
        {
            return new GooglePlaceDetails(
                searchResult.PlaceId,
                searchResult.ResourceName,
                attributions,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        return new GooglePlaceDetails(
            detailsPayload.PlaceId,
            detailsPayload.ResourceName,
            attributions,
            detailsPayload.FormattedAddress,
            detailsPayload.ShortFormattedAddress,
            detailsPayload.AddressDescriptor,
            detailsPayload.AdrFormatAddress,
            detailsPayload.Location,
            detailsPayload.Viewport,
            detailsPayload.Types);
    }

    private static IReadOnlyList<string>? CombineAttributions(
        IReadOnlyList<string>? searchAttributions,
        IReadOnlyList<string>? detailsAttributions)
    {
        static void AppendDistinct(List<string> target, IReadOnlyList<string>? source, HashSet<string> seen)
        {
            if (source is null)
            {
                return;
            }

            foreach (var entry in source)
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    continue;
                }

                if (seen.Add(entry))
                {
                    target.Add(entry);
                }
            }
        }

        if ((searchAttributions is null || searchAttributions.Count == 0) &&
            (detailsAttributions is null || detailsAttributions.Count == 0))
        {
            return null;
        }

        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        AppendDistinct(results, searchAttributions, seen);
        AppendDistinct(results, detailsAttributions, seen);

        return results.Count == 0 ? null : results;
    }
}
