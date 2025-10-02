using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Tripperist.MapsListConverter.App.Localization;
using Tripperist.MapsListConverter.Core.Lists;

namespace Tripperist.MapsListConverter.Service.Scraping;

/// <summary>
/// Uses Microsoft Playwright to simulate user interaction with Google Maps saved lists and extract the
/// resulting DOM once every entry has been loaded.
/// </summary>
public sealed class PlaywrightMapsListScraper(ResourceCatalog resources, ILogger<PlaywrightMapsListScraper> logger) : IMapsListScraper
{
    private readonly ResourceCatalog _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    private readonly ILogger<PlaywrightMapsListScraper> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<MapsList> LoadAsync(Uri listUri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(listUri);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(_resources.Log("ScrapingList", CultureInfo.CurrentCulture), listUri);

        using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
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
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60000
        }).ConfigureAwait(false);

        const string listSelector = "div[role='feed']";
        var listElement = await page.WaitForSelectorAsync(listSelector, new PageWaitForSelectorOptions
        {
            Timeout = 60000
        }).ConfigureAwait(false);

        if (listElement is null)
        {
            throw new TimeoutException(_resources.Error("ScrapingTimeout", CultureInfo.CurrentCulture));
        }

        await EnsureAllEntriesLoadedAsync(page, listSelector, cancellationToken).ConfigureAwait(false);

        const string evaluationScript = """
selector => {
    const container = document.querySelector(selector);
    const heading = document.querySelector('h1[role="heading"], h1');
    const description = document.querySelector('[aria-label="List description"], div[data-tooltip="List description"], div[jsaction*="description"]');
    const creator = document.querySelector('[aria-label^="Created by"], [aria-label*="Creator"], [data-tooltip^="Created by"]');
    const places = [];
    if (container) {
        const articles = Array.from(container.querySelectorAll('[role="article"]'));
        for (const article of articles) {
            const nameEl = article.querySelector('[role="link"] [role="heading"], [aria-level="3"], a[href] div[role="heading"]');
            const name = nameEl ? nameEl.textContent.trim() : '';
            const ratingEl = article.querySelector('[aria-label*="stars"], [aria-label*="Rated"], span[aria-hidden="true"]');
            let rating = null;
            if (ratingEl) {
                const aria = ratingEl.getAttribute('aria-label') ?? ratingEl.textContent;
                const match = aria?.match(/([0-9]+[.,][0-9]+)/);
                if (match) {
                    rating = parseFloat(match[1].replace(',', '.'));
                }
            }
            let reviews = null;
            const reviewCandidates = Array.from(article.querySelectorAll('span')).map(el => el.textContent?.trim() ?? '');
            for (const candidate of reviewCandidates) {
                if (/reviews?/i.test(candidate)) {
                    const digits = candidate.replace(/[^0-9]/g, '');
                    if (digits) {
                        reviews = parseInt(digits, 10);
                        break;
                    }
                }
            }
            let note = null;
            const noteLabel = article.querySelector('[aria-label="Note"]');
            if (noteLabel && noteLabel.nextElementSibling) {
                note = noteLabel.nextElementSibling.textContent?.trim() ?? null;
            }
            if (!note) {
                const noteCandidate = article.querySelector('[data-item-id^="note"]');
                if (noteCandidate) {
                    note = noteCandidate.textContent?.trim() ?? null;
                }
            }
            const imageEl = article.querySelector('img');
            const image = imageEl ? imageEl.src : null;
            places.push({ name, rating, reviews, note, image });
        }
    }
    return JSON.stringify({
        name: heading ? heading.textContent.trim() : '',
        description: description ? description.textContent.trim() : null,
        creator: creator ? creator.textContent.trim() : null,
        places
    });
}
""";

        var json = await page.EvaluateAsync<string>(evaluationScript, listSelector).ConfigureAwait(false);

        var raw = JsonSerializer.Deserialize<RawList>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var mapsList = new MapsList
        {
            Name = raw?.Name ?? string.Empty,
            Description = raw?.Description,
            Creator = raw?.Creator,
            Places = raw?.Places?.Select(place => new MapsPlace
            {
                Name = place?.Name ?? string.Empty,
                Rating = place?.Rating,
                ReviewCount = place?.Reviews,
                Note = place?.Note,
                ImageUrl = place?.Image
            }).ToList() ?? new List<MapsPlace>()
        };

        return mapsList;
    }

    private async Task EnsureAllEntriesLoadedAsync(IPage page, string selector, CancellationToken cancellationToken)
    {
        var previousCount = 0;
        var stableIterations = 0;

        while (!cancellationToken.IsCancellationRequested && stableIterations < 3)
        {
            var count = await page.EvaluateAsync<int>("""
selector => {
    const container = document.querySelector(selector);
    if (!container) {
        return 0;
    }
    container.scrollTo(0, container.scrollHeight);
    return container.querySelectorAll('[role="article"]').length;
}
""", selector).ConfigureAwait(false);

            if (count == previousCount)
            {
                stableIterations++;
            }
            else
            {
                stableIterations = 0;
                previousCount = count;
                _logger.LogDebug(_resources.Log("ScrollingBatch", CultureInfo.CurrentCulture), count);
            }

            await page.WaitForTimeoutAsync(500).ConfigureAwait(false);
        }
    }

    private sealed record RawList(string? Name, string? Description, string? Creator, RawPlace[]? Places);

    private sealed record RawPlace(string? Name, double? Rating, int? Reviews, string? Note, string? Image);
}
