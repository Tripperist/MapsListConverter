using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Tripperist.Core.GoogleMaps;
using Tripperist.Core.Localization;

namespace Tripperist.Service.GoogleMaps;

/// <summary>
/// Uses Microsoft.Playwright to load a Google Maps saved list page, scroll through all entries and convert the DOM into structured data.
/// </summary>
public sealed class PlaywrightListScrapingService(IPlaywrightFactory playwrightFactory, ILogger<PlaywrightListScrapingService> logger) : IListScrapingService
{
    private readonly IPlaywrightFactory _playwrightFactory = playwrightFactory ?? throw new ArgumentNullException(nameof(playwrightFactory));
    private readonly ILogger<PlaywrightListScrapingService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ResourceManager _logMessages = ResourceCatalog.LogMessages;
    private readonly ResourceManager _errorMessages = ResourceCatalog.ErrorMessages;

    /// <inheritdoc />
    public async Task<GoogleMapsList> ScrapeAsync(Uri listUri, bool verboseLogging, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(listUri);

        // We resolve resource strings lazily so that missing resources become obvious during logging.
        _logger.LogInformation(_logMessages.GetString("ScrapeStarting"), listUri);

        using var playwright = await _playwrightFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        }).ConfigureAwait(false);

        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = CultureInfo.CurrentCulture.Name
        }).ConfigureAwait(false);

        var page = await context.NewPageAsync().ConfigureAwait(false);
        await page.GotoAsync(listUri.ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        }).ConfigureAwait(false);

        //await EnsureAllPlacesLoadedAsync(page, verboseLogging, cancellationToken).ConfigureAwait(false);

        // Use a locator to find all <script> tags with a 'nonce' attribute.
        var scriptLocator = page.Locator("script[nonce]").Nth(1);


        // Wait for the filtered locator to be available and get its inner text.
        string scriptContent = await scriptLocator.InnerTextAsync();

        // Check if the script was found and print its content.
        if (scriptContent != null)
        {
            System.Console.WriteLine("Found script content:");
            System.Console.WriteLine(scriptContent);
        }
        else
        {
            System.Console.WriteLine("Script with matching content not found.");
        }

        var result = await ExtractListAsync(page, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(_logMessages.GetString("ParsingCompleted"), result.Name, result.Places.Count);
        return result;
    }

    private async Task EnsureAllPlacesLoadedAsync(IPage page, bool verboseLogging, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        ILocator feedLocator = page.Locator("div[role='main']");
        await feedLocator.First.WaitForAsync(new LocatorWaitForOptions
        {
            Timeout = 30_000
        }).ConfigureAwait(false);

        var articleLocator = feedLocator.Locator("div.m6QErb.XiKgde");
        var stableIterations = 0;
        var iteration = 0;

        // We stop scrolling only after the feed reports the same number of entries multiple times in a row.
        // This defensive strategy shields us from transient lazy-loading delays and avoids missing the final items.
        while (stableIterations < 3)
        {
            cancellationToken.ThrowIfCancellationRequested();
            iteration++;

            var beforeCount = await articleLocator.CountAsync().ConfigureAwait(false);

            await feedLocator.EvaluateAsync("element => element.scrollTo({ top: element.scrollHeight, behavior: 'instant' })").ConfigureAwait(false);
            await page.WaitForTimeoutAsync(750).ConfigureAwait(false);

            var afterCount = await articleLocator.CountAsync().ConfigureAwait(false);
            if (verboseLogging)
            {
                _logger.LogDebug(_logMessages.GetString("ScrollProgress"), iteration, afterCount);
            }

            if (afterCount <= beforeCount)
            {
                stableIterations++;

                // When the list stops growing we try to click any "Show more" button to load additional sections.
                var showMore = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Show more" });
                if (await showMore.CountAsync().ConfigureAwait(false) > 0)
                {
                    await showMore.First.ClickAsync().ConfigureAwait(false);
                    await page.WaitForTimeoutAsync(750).ConfigureAwait(false);
                    stableIterations = 0;
                    continue;
                }
            }
            else
            {
                stableIterations = 0;
            }

            if (iteration > 100)
            {
                break;
            }
        }

        var finalCount = await articleLocator.CountAsync().ConfigureAwait(false);
        _logger.LogInformation(_logMessages.GetString("ScrollCompleted"), finalCount);
    }

    private async Task<GoogleMapsList> ExtractListAsync(IPage page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);
        cancellationToken.ThrowIfCancellationRequested();

        var dto = await page.EvaluateAsync<ScrapedListDto?>(@"() => {
            const sanitize = value => {
                if (!value) {
                    return null;
                }

                const trimmed = value.trim();
                return trimmed.length === 0 ? null : trimmed;
            };

            const main = document.querySelector('[role=\""main\""]') ?? document.body;
            const feed = document.querySelector('div[role=\""main\""]');

            const places = [];
            if (feed) {
                const articles = Array.from(feed.querySelectorAll('div.m6QErb.XiKgde'));
                for (const article of articles) {
                    const nameElement = article.querySelector('div.fontHeadlineSmall.rZF81c');
                    const noteElement = article.querySelector('textarea[aria-label=\""Notes\""]');
                    const imageElement = article.querySelector('img');
                    const ratingElement = article.querySelector('span.MW4etd[aria-hidden*=\""true\""]');
                    const reviewElement = article.querySelector('span.UY7F9[aria-hidden*=\""true\""]');

                    let rating = null;
                    let reviewCount = null;
                    if (ratingElement && ratingElement.getAttribute('aria-label')) {
                        const aria = ratingElement.getAttribute('aria-label');
                        const ratingMatch = aria.match(/([0-9]+[.,]?[0-9]*)\s+stars/i);
                        if (ratingMatch) {
                            rating = Number(ratingMatch[1].replace(',', '.'));
                            if (!Number.isFinite(rating)) {
                                rating = null;
                            }
                        }

                        const reviewMatch = aria.match(/([0-9.,\s]+)\s+review/i);
                        if (reviewMatch) {
                            const digits = reviewMatch[1].replace(/[^0-9]/g, '');
                            if (digits.length > 0) {
                                reviewCount = Number(digits);
                                if (!Number.isFinite(reviewCount)) {
                                    reviewCount = null;
                                }
                            }
                        }
                    }

                    if (reviewCount === null && reviewElement) {
                        const text = reviewElement.textContent ?? '';
                        const digits = text.replace(/[^0-9]/g, '');
                        if (digits.length > 0) {
                            const parsed = Number(digits);
                            if (Number.isFinite(parsed)) {
                                reviewCount = parsed;
                            }
                        }
                    }

                    const imageUrl = imageElement && imageElement.src && !imageElement.src.startsWith('data:')
                        ? imageElement.src
                        : null;

                    places.push({
                        name: sanitize(nameElement?.textContent ?? null),
                        note: sanitize(noteElement?.textContent ?? null),
                        imageUrl,
                        rating,
                        reviewCount
                    });
                }
            }

            const titleElement = main.querySelector('span.WNNZR.fontTitleLarge');
            const descriptionElement = main.querySelector('[aria-label=\""List description\""], [data-section-id=\""description\""]');
            const creatorElement = main.querySelector('a[href*=\""/maps/contrib\""], [data-section-id=\""owner\""] span');

            return {
                name: sanitize(titleElement?.textContent ?? document.title ?? null),
                description: sanitize(descriptionElement?.textContent ?? null),
                creator: sanitize(creatorElement?.textContent ?? null),
                places
            };
        }").ConfigureAwait(false);

        if (dto is null || string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException(_errorMessages.GetString("ListParsingFailed"));
        }

        var places = dto.Places
            ?.Where(place => !string.IsNullOrWhiteSpace(place.Name))
            .Select(place => new GoogleMapsPlace(
                place.Name!,
                place.ImageUrl,
                place.Rating,
                place.ReviewCount,
                place.Note,
                null,
                null,
                null,
                null))
            .ToList()
            ?? new List<GoogleMapsPlace>();

        return new GoogleMapsList(dto.Name!, dto.Description, dto.Creator, places);
    }

    private sealed record ScrapedListDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("creator")]
        public string? Creator { get; init; }

        [JsonPropertyName("places")]
        public IReadOnlyList<ScrapedPlaceDto>? Places { get; init; }
    }

    private sealed record ScrapedPlaceDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; init; }

        [JsonPropertyName("rating")]
        public double? Rating { get; init; }

        [JsonPropertyName("reviewCount")]
        public int? ReviewCount { get; init; }

        [JsonPropertyName("note")]
        public string? Note { get; init; }
    }
}
