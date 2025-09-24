using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Tripperist.MapsListConverter.Models;

namespace Tripperist.MapsListConverter.Services;

/// <summary>
/// HTTP client wrapper responsible for invoking Google Places APIs using the Essentials SKU.
/// </summary>
public sealed class GooglePlacesClient(HttpClient httpClient, ILogger<GooglePlacesClient> logger, string apiKey) : IGooglePlacesClient
{
    private const string ApiKeyHeader = "X-Goog-Api-Key";
    private const string FieldMaskHeader = "X-Goog-FieldMask";
    private const string TextSearchFieldMask = "places.id,places.name";
    private const string PlaceDetailsFieldMask = "formattedAddress,location";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = ConfigureHttpClient(httpClient, apiKey);
    private readonly ILogger<GooglePlacesClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<GooglePlaceSearchResult?> SearchAsync(TMapsPlace place, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(place);

        var textQuery = BuildTextQuery(place);
        if (string.IsNullOrWhiteSpace(textQuery))
        {
            _logger.LogWarning("Skipping Google Places search for '{PlaceName}' because no name or address is available.", place.Name);
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/places:searchText")
        {
            Content = new StringContent(JsonSerializer.Serialize(new TextSearchRequest(textQuery), SerializerOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Add(FieldMaskHeader, TextSearchFieldMask);

        _logger.LogDebug("Searching Google Places for query '{Query}'.", textQuery);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("Google Places Text Search failed with status {StatusCode}: {Message}.", (int)response.StatusCode, errorContent);
            return null;
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<TextSearchResponse>(contentStream, SerializerOptions, cancellationToken).ConfigureAwait(false);
        if (payload?.Places is null || payload.Places.Count == 0)
        {
            _logger.LogWarning("Google Places Text Search returned no results for query '{Query}'.", textQuery);
            return null;
        }

        var match = payload.Places[0];
        if (string.IsNullOrWhiteSpace(match.Id) || string.IsNullOrWhiteSpace(match.Name))
        {
            _logger.LogWarning("Google Places Text Search response was missing identifier fields for query '{Query}'.", textQuery);
            return null;
        }

        return new GooglePlaceSearchResult(match.Id, match.Name);
    }

    /// <inheritdoc />
    public async Task<GooglePlaceDetails?> GetDetailsAsync(string placeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(placeId))
        {
            throw new ArgumentException("Place ID must be supplied when requesting details.", nameof(placeId));
        }

        var requestUri = FormattableString.Invariant($"v1/places/{Uri.EscapeDataString(placeId)}");
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add(FieldMaskHeader, PlaceDetailsFieldMask);

        _logger.LogDebug("Retrieving Google Place details for {PlaceId}.", placeId);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("Google Place Details request for {PlaceId} failed with status {StatusCode}: {Message}.", placeId, (int)response.StatusCode, errorContent);
            return null;
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<PlaceDetailsResponse>(contentStream, SerializerOptions, cancellationToken).ConfigureAwait(false);
        if (payload is null)
        {
            _logger.LogWarning("Google Place Details response for {PlaceId} was empty.", placeId);
            return null;
        }

        var latitude = payload.Location?.Latitude;
        var longitude = payload.Location?.Longitude;
        return new GooglePlaceDetails(payload.FormattedAddress, latitude, longitude);
    }

    private static string? BuildTextQuery(TMapsPlace place)
    {
        var parts = new List<string>(2);

        if (!string.IsNullOrWhiteSpace(place.Name))
        {
            parts.Add(place.Name);
        }

        if (!string.IsNullOrWhiteSpace(place.Address))
        {
            parts.Add(place.Address);
        }

        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    private static HttpClient ConfigureHttpClient(HttpClient? client, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(client);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key must be provided for Google Places requests.", nameof(apiKey));
        }

        if (client.BaseAddress is null)
        {
            client.BaseAddress = new Uri("https://places.googleapis.com/");
        }

        client.DefaultRequestHeaders.Remove(ApiKeyHeader);
        client.DefaultRequestHeaders.Add(ApiKeyHeader, apiKey);

        if (!client.DefaultRequestHeaders.Accept.Any(header => header.MediaType == "application/json"))
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        return client;
    }

    private sealed record TextSearchRequest([property: JsonPropertyName("textQuery")] string TextQuery);

    private sealed record TextSearchResponse([property: JsonPropertyName("places")] List<TextSearchPlace> Places);

    private sealed record TextSearchPlace(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("name")] string? Name);

    private sealed record PlaceDetailsResponse(
        [property: JsonPropertyName("formattedAddress")] string? FormattedAddress,
        [property: JsonPropertyName("location")] LatLng? Location);

    private sealed record LatLng(
        [property: JsonPropertyName("latitude")] double? Latitude,
        [property: JsonPropertyName("longitude")] double? Longitude);
}
