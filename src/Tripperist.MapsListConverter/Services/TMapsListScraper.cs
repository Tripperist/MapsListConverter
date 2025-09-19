using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Tripperist.MapsListConverter.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Tripperist.MapsListConverter.Services;

/// <summary>
/// Retrieves a Tripperist Maps list page and translates it into strongly typed models.
/// </summary>
public sealed class TMapsListScraper
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

        using var response = await _httpClient.GetAsync(listUri, HttpCompletionOption.ResponseContentRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        // HtmlAgilityPack gives us a tolerant DOM-like API which is extremely helpful when working with complex pages.
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(htmlContent);

        var scriptContent = ExtractInitializationScript(htmlDocument.DocumentNode);
        if (scriptContent is null)
        {
            throw new InvalidOperationException(
                "Unable to locate the window.APP_INITIALIZATION_STATE script in the retrieved HTML response.");
        }

        var jsonPayload = ExtractJsonPayload(scriptContent);
        if (jsonPayload is null)
        {
            throw new InvalidOperationException("Failed to isolate the APP_INITIALIZATION_STATE JSON payload.");
        }

        var rootNode = JsonNode.Parse(jsonPayload) ?? throw new InvalidOperationException("Empty initialization payload.");
        var listNode = FindListNode(rootNode);
        if (listNode is null)
        {
            throw new InvalidOperationException("Could not locate the list details within the initialization payload.");
        }

        var listData = ParseListNode(listNode);
        _logger.LogInformation("Extracted {Count} places from list '{ListName}'.", listData.Places.Count, listData.Name);
        return listData;
    }

    /// <summary>
    /// Searches the DOM for the script element that hosts the initialization data. We use the DOM instead of string search
    /// to remain resilient to minification or formatting changes in Google's HTML.
    /// </summary>
    private string? ExtractInitializationScript(HtmlNode documentNode)
    {
        foreach (var scriptNode in documentNode.SelectNodes("//script") ?? Enumerable.Empty<HtmlNode>())
        {
            var text = scriptNode.InnerText;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (text.Contains("window.APP_INITIALIZATION_STATE", StringComparison.Ordinal))
            {
                _logger.LogDebug("Found the script node that contains the initialization state block.");
                return text;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the JSON array assigned to the global <c>window.APP_INITIALIZATION_STATE</c> variable.
    /// </summary>
    private static string? ExtractJsonPayload(string scriptContent)
    {
        const string marker = "window.APP_INITIALIZATION_STATE=";

        var markerIndex = scriptContent.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return null;
        }

        var startIndex = scriptContent.IndexOf('[', markerIndex + marker.Length);
        if (startIndex < 0)
        {
            return null;
        }

        var builder = new StringBuilder();
        var depth = 0;
        var inString = false;
        var isEscaped = false;

        for (var index = startIndex; index < scriptContent.Length; index++)
        {
            var character = scriptContent[index];
            builder.Append(character);

            if (inString)
            {
                if (isEscaped)
                {
                    isEscaped = false;
                }
                else if (character == '\\')
                {
                    isEscaped = true;
                }
                else if (character == '"')
                {
                    inString = false;
                }

                continue;
            }

            switch (character)
            {
                case '"':
                    inString = true;
                    break;

                case '[':
                    depth++;
                    break;

                case ']':
                    depth--;
                    if (depth == 0)
                    {
                        return builder.ToString();
                    }

                    break;
            }
        }

        return null;
    }

    /// <summary>
    /// Recursively traverses the initialization payload looking for the entry that contains the list metadata and places.
    /// </summary>
    private JsonArray? FindListNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonArray array when LooksLikeListContainer(array):
                _logger.LogDebug("Found candidate list container with {Count} elements.", array.Count);
                return array;

            case JsonArray array:
                foreach (var child in array)
                {
                    if (child is JsonNode childNode)
                    {
                        var result = FindListNode(childNode);
                        if (result is not null)
                        {
                            return result;
                        }
                    }
                }

                break;

            case JsonObject obj:
                foreach (var property in obj)
                {
                    if (property.Value is JsonNode childNode)
                    {
                        var result = FindListNode(childNode);
                        if (result is not null)
                        {
                            return result;
                        }
                    }
                }

                break;
        }

        return null;
    }

    /// <summary>
    /// Converts the raw JSON array into domain models that are easier for the rest of the application to consume.
    /// </summary>
    private TMapsListData ParseListNode(JsonArray listNode)
    {
        if (listNode.Count == 0 || listNode[0] is not JsonArray innerArray)
        {
            throw new InvalidOperationException("Could not find the expected inner array in listNode.");
        }

        var creator = innerArray.Count > 3 && innerArray[3] is JsonArray creatorArray
            ? TryGetString(GetElement(creatorArray, 0))
            : null;

        var name = TryGetString(GetElement(innerArray, 4));
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Unable to determine the list name.");
        }

        var description = TryGetString(GetElement(innerArray, 5));
        var places = ParsePlaces(innerArray);

        return new TMapsListData(name, description, creator, places);
    }

    /// <summary>
    /// Extracts individual pieces of data from a place entry. The Google payload is largely positional, so we defensively
    /// check the indices before reading anything.
    /// </summary>
    private TMapsPlace? ParsePlaceNode(JsonArray placeArray)
    {
        var name = TryGetString(GetElement(placeArray, 2));
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogDebug("Encountered a place entry without a name. Skipping it to keep the KML clean.");
            return null;
        }

        var notes = TryGetString(GetElement(placeArray, 3));

        string? address = null;
        double? latitude = null;
        double? longitude = null;

        if (placeArray.Count > 1 && placeArray[1] is JsonArray locationArray)
        {
            address = TryGetString(GetElement(locationArray, 4));

            if (locationArray.Count > 5 && locationArray[5] is JsonArray coordinatesArray)
            {
                latitude = TryGetDouble(coordinatesArray, 2);
                longitude = TryGetDouble(coordinatesArray, 3);
            }
        }

        return new TMapsPlace(name, address, notes, latitude, longitude);
    }

    private IReadOnlyList<TMapsPlace> ParsePlaces(JsonArray innerArray)
    {
        if (innerArray.Count <= 8 || innerArray[8] is not JsonArray placesArray)
        {
            return Array.Empty<TMapsPlace>();
        }

        var places = new List<TMapsPlace>();

        foreach (var placeNode in placesArray)
        {
            if (placeNode is JsonArray placeArray)
            {
                var place = ParsePlaceNode(placeArray);
                if (place is not null)
                {
                    places.Add(place);
                }
            }
        }

        return places;
    }

    private static JsonNode? GetElement(JsonArray array, int index) => index < array.Count ? array[index] : null;

    private static string? TryGetString(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static double? TryGetDouble(JsonArray array, int index)
    {
        if (array.Count > index && array[index] is JsonValue value && value.TryGetValue<double>(out var number))
        {
            return number;
        }

        return null;
    }

    private static bool LooksLikeListContainer(JsonArray array)
    {
        if (array.Count == 0 || array[0] is not JsonArray innerArray)
        {
            return false;
        }

        if (innerArray.Count <= 8)
        {
            return false;
        }

        var name = TryGetString(GetElement(innerArray, 4));
        return !string.IsNullOrWhiteSpace(name) && innerArray[8] is JsonArray;
    }
}
