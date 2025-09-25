using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tripperist.MapsListConverter.Models;

namespace Tripperist.MapsListConverter.Services.GooglePlaces;

/// <summary>
/// HTTP client wrapper that communicates with the Google Places Text Search and Details APIs.
/// </summary>
public sealed class GooglePlacesClient : IGooglePlacesClient
{
    private const string TextSearchFieldMask = "places.attributions,places.id,places.name,nextPageToken";
    private const string DetailsFieldMask = "id,attributions,name,formattedAddress,shortFormattedAddress,addressDescriptor,adrFormatAddress,location,types,viewport";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<GooglePlacesClient> _logger;
    private readonly string _apiKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="GooglePlacesClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue requests.</param>
    /// <param name="logger">Logger instance used to record diagnostic information.</param>
    /// <param name="apiKey">Google Places API key.</param>
    public GooglePlacesClient(HttpClient httpClient, ILogger<GooglePlacesClient> logger, string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = string.IsNullOrWhiteSpace(apiKey)
            ? throw new ArgumentException("An API key must be supplied to query Google Places.", nameof(apiKey))
            : apiKey;
    }

    /// <inheritdoc />
    public async Task<GooglePlaceSearchResult?> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("A search query must be provided.", nameof(query));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/places:searchText");
        request.Headers.TryAddWithoutValidation("X-Goog-Api-Key", _apiKey);
        request.Headers.TryAddWithoutValidation("X-Goog-FieldMask", TextSearchFieldMask);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new TextSearchRequest(query), SerializerOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<TextSearchResponse>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        if (payload?.Places is null || payload.Places.Count == 0)
        {
            _logger.LogWarning("No Google Places matches were returned for query '{Query}'.", query);
            return null;
        }

        var match = payload.Places.FirstOrDefault(place =>
            !string.IsNullOrWhiteSpace(place.Id) &&
            !string.IsNullOrWhiteSpace(place.Name));

        if (match is null)
        {
            _logger.LogWarning("Google Places response for query '{Query}' did not contain any usable results.", query);
            return null;
        }

        var attributions = ExtractAttributions(match.Attributions);
        return new GooglePlaceSearchResult(match.Id!, match.Name!, attributions);
    }

    /// <inheritdoc />
    public async Task<GooglePlaceDetailsPayload?> GetDetailsAsync(string placeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(placeId))
        {
            throw new ArgumentException("A place identifier must be provided.", nameof(placeId));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"v1/places/{Uri.EscapeDataString(placeId)}");
        request.Headers.TryAddWithoutValidation("X-Goog-Api-Key", _apiKey);
        request.Headers.TryAddWithoutValidation("X-Goog-FieldMask", DetailsFieldMask);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Google Places returned 404 for place identifier '{PlaceId}'.", placeId);
            return null;
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<PlaceDetailsResponse>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        if (payload is null || string.IsNullOrWhiteSpace(payload.Id) || string.IsNullOrWhiteSpace(payload.Name))
        {
            _logger.LogWarning("Google Places details response for '{PlaceId}' did not contain the expected identifiers.", placeId);
            return null;
        }

        var attributions = ExtractAttributions(payload.Attributions);
        var location = payload.Location is { Latitude: double latitude, Longitude: double longitude }
            ? new GooglePlaceLocation(latitude, longitude)
            : null;

        var viewport = payload.Viewport is { Low: { Latitude: double lowLat, Longitude: double lowLng }, High: { Latitude: double highLat, Longitude: double highLng } }
            ? new GooglePlaceViewport(new GooglePlaceLocation(lowLat, lowLng), new GooglePlaceLocation(highLat, highLng))
            : null;

        return new GooglePlaceDetailsPayload(
            payload.Id!,
            payload.Name!,
            attributions,
            payload.FormattedAddress,
            payload.ShortFormattedAddress,
            payload.AddressDescriptor,
            payload.AdrFormatAddress,
            location,
            viewport,
            payload.Types);
    }

    private static IReadOnlyList<string>? ExtractAttributions(JsonElement? attributionsElement)
    {
        if (!attributionsElement.HasValue)
        {
            return null;
        }

        var element = attributionsElement.Value;
        if (element.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var results = new List<string>();
        foreach (var attribution in element.EnumerateArray())
        {
            results.Add(attribution.GetRawText());
        }

        return results;
    }

    private sealed record TextSearchRequest(string TextQuery);

    private sealed class TextSearchResponse
    {
        public List<TextSearchPlace>? Places { get; set; }

        public string? NextPageToken { get; set; }
    }

    private sealed class TextSearchPlace
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public JsonElement? Attributions { get; set; }
    }

    private sealed class PlaceDetailsResponse
    {
        public string? Id { get; set; }

        public JsonElement? Attributions { get; set; }

        public string? Name { get; set; }

        public string? FormattedAddress { get; set; }

        public string? ShortFormattedAddress { get; set; }

        public string? AddressDescriptor { get; set; }

        public string? AdrFormatAddress { get; set; }

        public LatLng? Location { get; set; }

        public Viewport? Viewport { get; set; }

        public List<string>? Types { get; set; }
    }

    private sealed class LatLng
    {
        public double? Latitude { get; set; }

        public double? Longitude { get; set; }
    }

    private sealed class Viewport
    {
        public LatLng? Low { get; set; }

        public LatLng? High { get; set; }
    }
}
