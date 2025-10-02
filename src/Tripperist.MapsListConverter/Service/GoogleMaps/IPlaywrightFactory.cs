using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Tripperist.Service.GoogleMaps;

/// <summary>
/// Abstracts the creation of Playwright instances to simplify testing and lifetime control.
/// </summary>
public interface IPlaywrightFactory
{
    /// <summary>
    /// Creates a new <see cref="IPlaywright"/> instance.
    /// </summary>
    /// <param name="cancellationToken">Token used to observe cancellation requests.</param>
    /// <returns>An initialized <see cref="IPlaywright"/>.</returns>
    Task<IPlaywright> CreateAsync(CancellationToken cancellationToken);
}
