using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Tripperist.Service.GoogleMaps;

/// <summary>
/// Default implementation that delegates to <see cref="Playwright.CreateAsync"/>.
/// </summary>
public sealed class PlaywrightFactory : IPlaywrightFactory
{
    /// <inheritdoc />
    public async Task<IPlaywright> CreateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        return playwright ?? throw new InvalidOperationException("Failed to initialize Playwright.");
    }
}
