using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Resources;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tripperist.Core.Configuration;
using Tripperist.Core.Localization;

namespace Tripperist.Service.GooglePlaces;

/// <summary>
/// Minimal wrapper around the Google Places REST API.
/// </summary>
public sealed class GooglePlacesClient(HttpClient httpClient, IOptions<GooglePlacesOptions> options, ILogger<GooglePlacesClient> logger)
    : IGooglePlacesClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly GooglePlacesOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<GooglePlacesClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ResourceManager _errorMessages = ResourceCatalog.ErrorMessages;

    /// <inheritdoc />
    public async Task<PlaceLookupResult?> LookupAsync(string placeName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(placeName))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(_errorMessages.GetString("GooglePlacesApiKeyMissing"));
        }

        var escapedName = Uri.EscapeDataString(placeName);
        var findPlaceUri = new Uri($"place/findplacefromtext/json?input={escapedName}&inputtype=textquery&fields=place_id&key={_options.ApiKey}", UriKind.Relative);

        using var lookupResponse = await _httpClient.GetAsync(findPlaceUri, cancellationToken).ConfigureAwait(false);
        lookupResponse.EnsureSuccessStatusCode();
        var lookupPayload = await lookupResponse.Content.ReadFromJsonAsync<FindPlaceResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        var candidate = lookupPayload?.Candidates?.FirstOrDefault();
        if (candidate is null || string.IsNullOrWhiteSpace(candidate.PlaceId))
        {
            return null;
        }

        var placeId = candidate.PlaceId;
        var detailsUri = new Uri($"place/details/json?place_id={Uri.EscapeDataString(placeId)}&fields=formatted_address,geometry/location&key={_options.ApiKey}", UriKind.Relative);
        using var detailsResponse = await _httpClient.GetAsync(detailsUri, cancellationToken).ConfigureAwait(false);
        detailsResponse.EnsureSuccessStatusCode();

        var detailsPayload = await detailsResponse.Content.ReadFromJsonAsync<PlaceDetailsResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        var location = detailsPayload?.Result?.Geometry?.Location;
        var address = detailsPayload?.Result?.FormattedAddress;

        _logger.LogDebug("Resolved {PlaceName} to place id {PlaceId}.", placeName, placeId);
        return new PlaceLookupResult(placeId, location?.Lat, location?.Lng, address);
    }

    private sealed record FindPlaceResponse
    {
        [JsonPropertyName("candidates")]
        public FindPlaceCandidate[]? Candidates { get; init; }
    }

    private sealed record FindPlaceCandidate
    {
        [JsonPropertyName("place_id")]
        public string? PlaceId { get; init; }
    }

    private sealed record PlaceDetailsResponse
    {
        [JsonPropertyName("result")]
        public PlaceDetailsResult? Result { get; init; }
    }

    private sealed record PlaceDetailsResult
    {
        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; init; }

        [JsonPropertyName("geometry")]
        public PlaceGeometry? Geometry { get; init; }
    }

    private sealed record PlaceGeometry
    {
        [JsonPropertyName("location")]
        public PlaceLocation? Location { get; init; }
    }

    private sealed record PlaceLocation
    {
        [JsonPropertyName("lat")]
        public double? Lat { get; init; }

        [JsonPropertyName("lng")]
        public double? Lng { get; init; }
    }
}
