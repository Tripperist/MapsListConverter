namespace Tripperist.MapsListConverter.Console.Commands;

/// <summary>
/// Dispatches command-line invocations to the appropriate handler.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Dispatches the command represented by <paramref name="args"/>.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Exit code resulting from the command execution.</returns>
    Task<int> DispatchAsync(string[] args, CancellationToken cancellationToken);
}
