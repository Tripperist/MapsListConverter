using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Tripperist.MapsListConverter.Models;

namespace Tripperist.MapsListConverter.Services;

/// <summary>
/// Browser-driven scraper that uses Playwright to collect detailed metadata for each place in a shared Google Maps list.
/// </summary>
public sealed class TMapsListBrowserScraper : IMapsListScraper
{
    private static readonly string[] ResultsSelectors =
    [
        "div[role='feed']",
        "div[aria-label*='Results' i]",
        "div[aria-label*='Places' i]"
    ];

    private static readonly Regex ReviewCountRegex = new("([0-9][0-9,\\.]*)", RegexOptions.Compiled);

    private readonly ILogger<TMapsListBrowserScraper> _logger;

    public TMapsListBrowserScraper(ILogger<TMapsListBrowserScraper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TMapsListData> FetchListAsync(Uri listUri, CancellationToken cancellationToken = default)
    {
        if (listUri is null)
        {
            throw new ArgumentNullException(nameof(listUri));
        }

        try
        {
            await using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                Locale = "en-US",
                UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36"
            });
            await using var page = await context.NewPageAsync();

            _logger.LogInformation("Launching headless Chromium to scrape list {Uri}.", listUri);

            await page.GotoAsync(listUri.ToString(), new PageGotoOptions
            {
                Timeout = 60000,
                WaitUntil = WaitUntilState.NetworkIdle
            });

            cancellationToken.ThrowIfCancellationRequested();

            var initializationJson = await page.EvaluateAsync<string?>(
                "() => window.APP_INITIALIZATION_STATE ? JSON.stringify(window.APP_INITIALIZATION_STATE) : null");

            if (string.IsNullOrWhiteSpace(initializationJson))
            {
                throw new InvalidOperationException("Unable to locate the window.APP_INITIALIZATION_STATE payload after navigation.");
            }

            var listData = TMapsListParser.ParseFromInitializationPayload(initializationJson, _logger);
            var enrichedPlaces = await EnrichPlacesAsync(page, listData.Places, cancellationToken).ConfigureAwait(false);

            return listData with { Places = enrichedPlaces };
        }
        catch (PlaywrightException ex)
        {
            _logger.LogError(ex, "Playwright failed to launch. Ensure browsers are installed via 'playwright install'.");
            throw new InvalidOperationException(
                "Unable to launch the Playwright browser runtime. Please run 'playwright install' before using the --detailed option.",
                ex);
        }
    }

    private async Task<IReadOnlyList<TMapsPlace>> EnrichPlacesAsync(
        IPage page,
        IReadOnlyList<TMapsPlace> basePlaces,
        CancellationToken cancellationToken)
    {
        if (basePlaces.Count == 0)
        {
            return basePlaces;
        }

        var resultsContainer = await LocateResultsContainerAsync(page, cancellationToken).ConfigureAwait(false);
        if (resultsContainer is null)
        {
            _logger.LogWarning("Failed to locate the results list in the browser view. Returning initialization payload data only.");
            return basePlaces;
        }

        await EnsureAllCardsLoadedAsync(page, resultsContainer, cancellationToken).ConfigureAwait(false);

        var cards = resultsContainer.Locator("div[role='article'], div[jsaction*='mouseover:pane']");
        var cardCount = await cards.CountAsync().ConfigureAwait(false);
        if (cardCount == 0)
        {
            _logger.LogWarning("No result cards were detected after scrolling the list. Returning initialization payload data only.");
            return basePlaces;
        }

        var enriched = new List<TMapsPlace>(basePlaces.Count);
        var limit = Math.Min(cardCount, basePlaces.Count);

        for (var index = 0; index < limit; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var card = cards.Nth(index);
            var basePlace = basePlaces[index];

            try
            {
                await card.ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
                await card.ClickAsync(new LocatorClickOptions { Timeout = 15000 }).ConfigureAwait(false);
                await WaitForDetailsPaneAsync(page, basePlace.Name, cancellationToken).ConfigureAwait(false);

                var detailed = await ExtractPlaceFromDetailPaneAsync(page, basePlace, cancellationToken).ConfigureAwait(false);
                enriched.Add(detailed);
            }
            catch (PlaywrightException ex)
            {
                _logger.LogWarning(ex, "Failed to extract extended metadata for place '{PlaceName}'. Using initialization payload data instead.", basePlace.Name);
                enriched.Add(basePlace);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse extended metadata for place '{PlaceName}'. Using initialization payload data instead.", basePlace.Name);
                enriched.Add(basePlace);
            }
        }

        for (var index = limit; index < basePlaces.Count; index++)
        {
            enriched.Add(basePlaces[index]);
        }

        return enriched;
    }

    private static async Task<ILocator?> LocateResultsContainerAsync(IPage page, CancellationToken cancellationToken)
    {
        foreach (var selector in ResultsSelectors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var handle = await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 5000
                }).ConfigureAwait(false);

                if (handle is not null)
                {
                    await handle.DisposeAsync().ConfigureAwait(false);
                    return page.Locator(selector).First;
                }
            }
            catch (TimeoutException)
            {
            }
            catch (PlaywrightException)
            {
            }
        }

        return null;
    }

    private static async Task EnsureAllCardsLoadedAsync(IPage page, ILocator container, CancellationToken cancellationToken)
    {
        var stableIterations = 0;
        var previousCount = -1;

        while (stableIterations < 3)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int childCount;
            try
            {
                childCount = await container.EvaluateAsync<int>("el => el && el.children ? el.children.length : 0").ConfigureAwait(false);
            }
            catch (PlaywrightException)
            {
                break;
            }

            if (childCount == previousCount)
            {
                stableIterations++;
            }
            else
            {
                stableIterations = 0;
                previousCount = childCount;
            }

            try
            {
                await container.EvaluateAsync("el => el && (el.scrollTop = el.scrollHeight)").ConfigureAwait(false);
                await container.FocusAsync(new LocatorFocusOptions { Timeout = 1000 }).ConfigureAwait(false);
                await container.PressAsync("End").ConfigureAwait(false);
            }
            catch (PlaywrightException)
            {
                // If the container is not focusable we still continue with scrollHeight adjustments.
            }

            await page.WaitForTimeoutAsync(400).ConfigureAwait(false);
        }
    }

    private static async Task WaitForDetailsPaneAsync(IPage page, string placeName, CancellationToken cancellationToken)
    {
        var detailHeading = page.Locator("div[role='main'] h1 span[role='text'], div[role='main'] h1");
        try
        {
            await detailHeading.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 15000
            }).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
        }
        catch (PlaywrightException)
        {
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(placeName))
        {
            try
            {
                await page.WaitForFunctionAsync(
                    @"({ selector, expected }) => {
                        const node = document.querySelector(selector);
                        if (!node) {
                            return false;
                        }
                        const text = (node.innerText || node.textContent || '').trim().toLowerCase();
                        return text.includes(expected);
                    }",
                    new { selector = "div[role='main'] h1", expected = placeName.ToLowerInvariant() },
                    new PageWaitForFunctionOptions { Timeout = 5000 }).ConfigureAwait(false);
            }
            catch (PlaywrightException)
            {
            }
        }
    }

    private async Task<TMapsPlace> ExtractPlaceFromDetailPaneAsync(IPage page, TMapsPlace basePlace, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var detailPane = page.Locator("div[role='main']");
        await detailPane.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 15000 }).ConfigureAwait(false);

        var detailsJson = await detailPane.EvaluateAsync<string>(@"el => {
            const getText = selector => {
                const node = el.querySelector(selector);
                if (!node) {
                    return null;
                }
                const value = (node.innerText || node.textContent || '').trim();
                return value.length ? value : null;
            };
            const getHref = selector => {
                const node = el.querySelector(selector);
                if (!node) {
                    return null;
                }
                return node.href || null;
            };
            const result = {
                name: getText('h1 span[role=\"text\"], h1 span, h1'),
                address: getText('[data-item-id=\"address\"] span:last-child, button[data-item-id=\"address\"] span:last-child'),
                rating: getText('[aria-label*=\"star\" i]'),
                reviews: getText('[aria-label*=\"review\" i]'),
                phone: getText('[data-item-id^=\"phone\"] span:last-child, [data-item-id^=\"phone\"] div[role=\"text\"]'),
                website: getHref('a[data-item-id=\"authority\"]'),
                openingHours: getText('[data-item-id=\"oh\"] span:last-child'),
                plusCode: getText('[data-item-id=\"oloc\"] span:last-child'),
                mapLink: getHref('a[href*=\"/maps/place\" i], a[href*=\"google.com/maps\" i]')
            };
            return JSON.stringify(result);
        }").ConfigureAwait(false);

        var details = JsonSerializer.Deserialize<Dictionary<string, string?>>(detailsJson) ?? new Dictionary<string, string?>();

        var name = Normalize(details.TryGetValue("name", out var rawName) ? rawName : null) ?? basePlace.Name;
        var address = Normalize(details.TryGetValue("address", out var rawAddress) ? rawAddress : null) ?? basePlace.Address;
        var phone = Normalize(details.TryGetValue("phone", out var rawPhone) ? rawPhone : null) ?? basePlace.Phone;
        var website = Normalize(details.TryGetValue("website", out var rawWebsite) ? rawWebsite : null) ?? basePlace.Website;
        var openingHours = Normalize(details.TryGetValue("openingHours", out var rawHours) ? rawHours : null) ?? basePlace.OpeningHours;
        var plusCode = Normalize(details.TryGetValue("plusCode", out var rawPlusCode) ? rawPlusCode : null) ?? basePlace.PlusCode;
        var rating = ParseNullableDouble(details.TryGetValue("rating", out var rawRating) ? rawRating : null) ?? basePlace.Rating;
        var reviewCount = ParseReviewCount(details.TryGetValue("reviews", out var rawReviews) ? rawReviews : null) ?? basePlace.ReviewCount;
        var mapLink = Normalize(details.TryGetValue("mapLink", out var rawMapLink) ? rawMapLink : null);

        var coordinates = ParseCoordinatesFromMapLink(mapLink);
        var latitude = coordinates.Latitude ?? basePlace.Latitude;
        var longitude = coordinates.Longitude ?? basePlace.Longitude;

        return new TMapsPlace(name, address, basePlace.Notes, latitude, longitude, rating, reviewCount, phone, website, openingHours, plusCode);
    }

    private static double? ParseNullableDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var candidate = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = value;
        }

        if (double.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariantResult))
        {
            return invariantResult;
        }

        if (double.TryParse(candidate, NumberStyles.Float, CultureInfo.CurrentCulture, out var cultureResult))
        {
            return cultureResult;
        }

        return null;
    }

    private static int? ParseReviewCount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = ReviewCountRegex.Match(value);
        if (!match.Success)
        {
            return null;
        }

        var digits = match.Groups[1].Value.Replace(",", string.Empty, StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal);
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private static (double? Latitude, double? Longitude) ParseCoordinatesFromMapLink(string? mapLink)
    {
        if (string.IsNullOrWhiteSpace(mapLink))
        {
            return (null, null);
        }

        var atIndex = mapLink.IndexOf("/@", StringComparison.Ordinal);
        if (atIndex >= 0)
        {
            var coordinateSection = mapLink[(atIndex + 2)..];
            var endIndex = coordinateSection.IndexOf('/', StringComparison.Ordinal);
            if (endIndex >= 0)
            {
                coordinateSection = coordinateSection[..endIndex];
            }

            var parts = coordinateSection.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2 &&
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
            {
                return (lat, lng);
            }
        }

        var queryIndex = mapLink.IndexOf("q=", StringComparison.OrdinalIgnoreCase);
        if (queryIndex >= 0)
        {
            var querySection = mapLink[(queryIndex + 2)..];
            var ampIndex = querySection.IndexOf('&');
            if (ampIndex >= 0)
            {
                querySection = querySection[..ampIndex];
            }

            var colonIndex = querySection.IndexOf(':');
            if (colonIndex >= 0)
            {
                querySection = querySection[(colonIndex + 1)..];
            }

            var parts = querySection.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2 &&
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
            {
                return (lat, lng);
            }
        }

        return (null, null);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = value.ReplaceLineEndings(" ").Trim();
        while (cleaned.Contains("  ", StringComparison.Ordinal))
        {
            cleaned = cleaned.Replace("  ", " ", StringComparison.Ordinal);
        }

        return cleaned.Length == 0 ? null : cleaned;
    }
}
