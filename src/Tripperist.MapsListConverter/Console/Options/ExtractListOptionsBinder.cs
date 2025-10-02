using System.Globalization;
using Tripperist.MapsListConverter.App.Localization;

namespace Tripperist.MapsListConverter.Console.Options;

/// <summary>
/// Binds raw command-line arguments into <see cref="ExtractListOptions"/> instances.
/// </summary>
public sealed class ExtractListOptionsBinder(ResourceCatalog resources) : ICommandOptionsBinder<ExtractListOptions>
{
    private readonly ResourceCatalog _resources = resources ?? throw new ArgumentNullException(nameof(resources));

    /// <inheritdoc />
    public ExtractListOptions Bind(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        Uri? inputUrl = null;
        string? kmlPath = null;
        var exportCsv = false;
        var verbose = false;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--input" or "-i":
                    EnsureValue(args, index, argument);
                    inputUrl = ParseUri(args[++index]);
                    break;
                case "--kml":
                    EnsureValue(args, index, argument);
                    kmlPath = args[++index];
                    break;
                case "--csv":
                    exportCsv = true;
                    break;
                case "--verbose":
                    verbose = true;
                    break;
            }
        }

        return new ExtractListOptions
        {
            InputUrl = inputUrl,
            KmlOutputPath = kmlPath ?? string.Empty,
            ExportCsv = exportCsv,
            Verbose = verbose
        };
    }

    private static void EnsureValue(string[] args, int index, string argument)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {argument}.");
        }
    }

    private Uri ParseUri(string candidate)
    {
        if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return uri;
        }

        var message = _resources.Error("MissingInputUrl", CultureInfo.CurrentCulture);
        throw new FormatException(message);
    }
}
