using System.Threading;
using System.Threading.Tasks;

namespace Tripperist.Core.Commands;

/// <summary>
/// Provides a common abstraction for console commands so they can share validation, logging and lifetime management.
/// </summary>
/// <typeparam name="TOptions">Type describing the parsed command line options.</typeparam>
public abstract class CommandHandler<TOptions>
{
    /// <summary>
    /// Executes the command asynchronously using the supplied options.
    /// </summary>
    /// <param name="options">Validated command line options.</param>
    /// <param name="cancellationToken">Token used to observe cancellation requests.</param>
    /// <returns>Zero for success, non-zero for failure.</returns>
    public abstract Task<int> ExecuteAsync(TOptions options, CancellationToken cancellationToken);
}
