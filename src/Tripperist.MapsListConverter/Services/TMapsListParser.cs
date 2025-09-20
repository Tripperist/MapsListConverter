using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Tripperist.MapsListConverter.Models;

namespace Tripperist.MapsListConverter.Services;

/// <summary>
/// Shared helper that translates the Tripperist/Google Maps initialization payload into strongly typed models.
/// </summary>
internal static class TMapsListParser
{
    public static TMapsListData ParseFromHtml(string htmlContent, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            throw new ArgumentException("HTML content must be provided.", nameof(htmlContent));
        }

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(htmlContent);

        var scriptContent = ExtractInitializationScript(htmlDocument.DocumentNode, logger);
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

        return ParseFromInitializationPayload(jsonPayload, logger);
    }

    public static TMapsListData ParseFromInitializationPayload(string jsonPayload, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(jsonPayload))
        {
            throw new ArgumentException("A non-empty initialization payload is required.", nameof(jsonPayload));
        }

        var rootNode = JsonNode.Parse(jsonPayload) ?? throw new InvalidOperationException("Empty initialization payload.");
        var listNode = FindListNode(rootNode, logger);
        if (listNode is null)
        {
            throw new InvalidOperationException("Could not locate the list details within the initialization payload.");
        }

        return ParseListNode(listNode, logger);
    }

    private static string? ExtractInitializationScript(HtmlNode documentNode, ILogger logger)
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
                logger.LogDebug("Found the script node that contains the initialization state block.");
                return text;
            }
        }

        return null;
    }

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

    private static JsonArray? FindListNode(JsonNode node, ILogger logger)
    {
        switch (node)
        {
            case JsonArray array:
            {
                foreach (var element in array)
                {
                    if (element is JsonArray candidate)
                    {
                        foreach (var item in candidate)
                        {
                            if (item is JsonValue value)
                            {
                                string? shareUrl = null;
                                try
                                {
                                    shareUrl = value.GetValue<string?>();
                                }
                                catch
                                {
                                }

                                if (!string.IsNullOrEmpty(shareUrl) &&
                                    shareUrl.Contains("https://www.google.com/maps/placelists/list/", StringComparison.Ordinal))
                                {
                                    logger.LogDebug("Identified share URL '{ShareUrl}' while traversing the payload.", shareUrl);

                                    string processed = shareUrl;
                                    var startIdx = processed.IndexOf("[[[\"", StringComparison.Ordinal);
                                    processed = startIdx >= 0 ? processed.Substring(startIdx) : processed;

                                    if (processed.EndsWith("\"", StringComparison.Ordinal))
                                    {
                                        processed = processed[..^1];
                                    }

                                    try
                                    {
                                        var jsonArray = JsonNode.Parse(processed) as JsonArray;
                                        if (jsonArray is not null)
                                        {
                                            return jsonArray;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.LogError(ex, "Failed to parse processed shareUrl string as JsonArray: {ShareUrl}", processed);
                                    }
                                }
                            }
                            else if (item is JsonArray nestedArray)
                            {
                                var result = FindListNode(nestedArray, logger);
                                if (result is not null)
                                {
                                    return result;
                                }
                            }
                        }
                    }
                }

                foreach (var child in array)
                {
                    if (child is JsonNode childNode)
                    {
                        var result = FindListNode(childNode, logger);
                        if (result is not null)
                        {
                            return result;
                        }
                    }
                }

                break;
            }

            case JsonObject obj:
                foreach (var property in obj)
                {
                    if (property.Value is JsonNode childNode)
                    {
                        var result = FindListNode(childNode, logger);
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

    private static TMapsListData ParseListNode(JsonArray listNode, ILogger logger)
    {
        if (listNode is null || listNode.Count == 0)
        {
            throw new InvalidOperationException("listNode is empty or null.");
        }

        var innerArray = listNode[0] as JsonArray ?? throw new InvalidOperationException("Could not find the expected inner array in listNode.");

        string? sharedUrl = null;
        if (innerArray.Count > 2 && innerArray[2] is JsonArray urlArray && urlArray.Count > 2)
        {
            sharedUrl = urlArray[2]?.GetValue<string?>();
        }

        string? creator = null;
        if (innerArray.Count > 3 && innerArray[3] is JsonArray creatorArray && creatorArray.Count > 0)
        {
            creator = creatorArray[0]?.GetValue<string?>();
        }

        string? name = null;
        if (innerArray.Count > 4)
        {
            name = innerArray[4]?.GetValue<string?>();
        }

        string? description = null;
        if (innerArray.Count > 5)
        {
            description = innerArray[5]?.GetValue<string?>();
        }

        var places = new List<TMapsPlace>();
        if (innerArray.Count > 8 && innerArray[8] is JsonArray placesArray)
        {
            foreach (var placeNode in placesArray)
            {
                if (placeNode is JsonArray placeArray)
                {
                    var place = ParsePlaceNode(placeArray, logger);
                    if (place is not null)
                    {
                        places.Add(place);
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Unable to determine the list name.");
        }

        logger.LogDebug("Parsed {PlaceCount} places from initialization payload for list '{ListName}'.", places.Count, name);
        return new TMapsListData(name, description, creator, places);
    }

    private static TMapsPlace? ParsePlaceNode(JsonArray placeArray, ILogger logger)
    {
        var name = placeArray.Count > 2 ? placeArray[2]?.GetValue<string?>() : null;
        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogDebug("Encountered a place entry without a name. Skipping it to keep the KML clean.");
            return null;
        }

        var notes = placeArray.Count > 3 ? placeArray[3]?.GetValue<string?>() : null;

        string? address = null;
        double? latitude = null;
        double? longitude = null;

        if (placeArray.Count > 1 && placeArray[1] is JsonArray locationArray)
        {
            address = locationArray.Count > 4 ? locationArray[4]?.GetValue<string?>() : null;

            if (locationArray.Count > 5 && locationArray[5] is JsonArray coordinatesArray)
            {
                latitude = TryGetDouble(coordinatesArray, 2);
                longitude = TryGetDouble(coordinatesArray, 3);
            }
        }

        return new TMapsPlace(name, address, notes, latitude, longitude);
    }

    private static double? TryGetDouble(JsonArray array, int index)
    {
        if (array.Count > index && array[index] is JsonValue value && value.TryGetValue<double>(out var number))
        {
            return number;
        }

        return null;
    }
}
