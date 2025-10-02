using Microsoft.Extensions.Logging;

namespace Tripperist.MapsListConverter.Console.Commands;

/// <summary>
/// Base class for command handlers. Provides consistent logging and cancellation handling.
/// </summary>
/// <typeparam name="TOptions">Type of options the handler consumes.</typeparam>
public abstract class CommandHandler<TOptions>
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandHandler{TOptions}"/> class.
    /// </summary>
    /// <param name="logger">Logger used to emit diagnostic information.</param>
    protected CommandHandler(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the command with the supplied options.
    /// </summary>
    /// <param name="options">Bound options.</param>
    /// <param name="cancellationToken">Cancellation token provided by the host.</param>
    /// <returns>Exit code representing success or failure.</returns>
    public Task<int> ExecuteAsync(TOptions options, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Command execution cancelled before start.");
            return Task.FromResult(1);
        }

        return ExecuteCoreAsync(options, cancellationToken);
    }

    /// <summary>
    /// Derived handlers implement their business logic here.
    /// </summary>
    /// <param name="options">Bound options.</param>
    /// <param name="cancellationToken">Cancellation token provided by the host.</param>
    /// <returns>Exit code representing success or failure.</returns>
    protected abstract Task<int> ExecuteCoreAsync(TOptions options, CancellationToken cancellationToken);
}
