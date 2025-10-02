using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using Tripperist.MapsListConverter.App.Localization;
using Tripperist.MapsListConverter.App.Validation;
using Tripperist.MapsListConverter.Console.Options;

namespace Tripperist.MapsListConverter.Console.Commands;

/// <summary>
/// Coordinates option binding, validation, and command execution.
/// </summary>
public sealed class CommandDispatcher(
    CommandHandler<ExtractListOptions> handler,
    ICommandOptionsBinder<ExtractListOptions> binder,
    IOptionsValidator<ExtractListOptions> validator,
    IUsageWriter usageWriter,
    ResourceCatalog resources,
    ILogger<CommandDispatcher> logger) : ICommandDispatcher
{
    private readonly CommandHandler<ExtractListOptions> _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    private readonly ICommandOptionsBinder<ExtractListOptions> _binder = binder ?? throw new ArgumentNullException(nameof(binder));
    private readonly IOptionsValidator<ExtractListOptions> _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    private readonly IUsageWriter _usageWriter = usageWriter ?? throw new ArgumentNullException(nameof(usageWriter));
    private readonly ResourceCatalog _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    private readonly ILogger<CommandDispatcher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<int> DispatchAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args is null || args.Length == 0 || args.Contains("--help", StringComparer.OrdinalIgnoreCase))
        {
            _usageWriter.Write();
            return 0;
        }

        ExtractListOptions options;
        try
        {
            options = _binder.Bind(args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            _usageWriter.Write();
            return 1;
        }
        var validationErrors = _validator.Validate(options);
        if (validationErrors.Count > 0)
        {
            _logger.LogError(_resources.Error("ValidationFailed", CultureInfo.CurrentCulture));
            foreach (var error in validationErrors)
            {
                _logger.LogError(" - {ValidationError}", error);
            }

            _usageWriter.Write();
            return 1;
        }

        return await _handler.ExecuteAsync(options, cancellationToken).ConfigureAwait(false);
    }
}
