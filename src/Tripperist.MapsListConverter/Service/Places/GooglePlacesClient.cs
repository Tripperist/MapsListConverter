using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tripperist.MapsListConverter.App.Localization;
using Tripperist.MapsListConverter.Core.Configuration;

namespace Tripperist.MapsListConverter.Service.Places;

/// <summary>
/// Google Places API client responsible for resolving Place IDs for scraped locations.
/// </summary>
public sealed class GooglePlacesClient(
    HttpClient httpClient,
    IOptions<GooglePlacesSettings> settings,
    ResourceCatalog resources,
    ILogger<GooglePlacesClient> logger) : IGooglePlacesClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly GooglePlacesSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ResourceCatalog _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    private readonly ILogger<GooglePlacesClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<string?> ResolvePlaceIdAsync(string placeName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(placeName))
        {
            return null;
        }

        var requestUri = BuildRequestUri(placeName);
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var error = string.Format(CultureInfo.CurrentCulture, _resources.Error("PlacesApiRequestFailed", CultureInfo.CurrentCulture), response.StatusCode);
            _logger.LogWarning(error);
            return null;
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var candidate in candidates.EnumerateArray())
        {
            if (candidate.TryGetProperty("place_id", out var placeIdProperty))
            {
                return placeIdProperty.GetString();
            }
        }

        return null;
    }

    private string BuildRequestUri(string placeName)
    {
        var builder = new UriBuilder("https://maps.googleapis.com/maps/api/place/findplacefromtext/json");
        var query = $"input={Uri.EscapeDataString(placeName)}&inputtype=textquery&fields=place_id&key={_settings.ApiKey}";
        builder.Query = query;
        return builder.Uri.ToString();
    }
}
