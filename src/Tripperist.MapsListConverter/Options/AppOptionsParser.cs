using System;
using System.IO;
using System.Linq;

namespace Tripperist.Console.ListExport.Options;

/// <summary>
/// Parses command line arguments into a strongly typed <see cref="AppOptions"/> instance.
/// </summary>
public static class AppOptionsParser
{
    private static readonly string[] HelpFlags = ["--help", "-h", "-?", "/?"];

    /// <summary>
    /// Determines whether the provided argument set is requesting help/usage information.
    /// </summary>
    public static bool ShouldShowHelp(string[] args) => args.Any(arg => HelpFlags.Contains(arg, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Attempts to parse the supplied arguments into a validated <see cref="AppOptions"/> instance.
    /// </summary>
    public static bool TryParse(string[] args, out AppOptions? options, out string? errorMessage)
    {
        string? inputUrlValue = null;
        string? kmlFileValue = null;
        var verbose = false;
        var exportCsv = false;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            if (HelpFlags.Contains(argument, StringComparer.OrdinalIgnoreCase))
            {
                // The help case is handled by the caller, so we quietly ignore it here to avoid noise.
                continue;
            }

            switch (argument)
            {
                case "--inputUrl":
                    if (++index >= args.Length)
                    {
                        errorMessage = "The --inputUrl option requires a value.";
                        options = null;
                        return false;
                    }

                    inputUrlValue = args[index];
                    break;

                case "--kml":
                    if (++index >= args.Length)
                    {
                        errorMessage = "The --kml option requires a value.";
                        options = null;
                        return false;
                    }

                    kmlFileValue = args[index];
                    break;

                case "--verbose":
                    verbose = true;
                    break;

                case "--csv":
                    exportCsv = true;
                    break;

                default:
                    if (argument.StartsWith("--", StringComparison.Ordinal))
                    {
                        errorMessage = $"Unknown option '{argument}'.";
                    }
                    else
                    {
                        errorMessage = $"Unexpected argument '{argument}'.";
                    }

                    options = null;
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(inputUrlValue))
        {
            errorMessage = "The --inputUrl argument is required.";
            options = null;
            return false;
        }

        if (!Uri.TryCreate(inputUrlValue, UriKind.Absolute, out var inputListUri))
        {
            errorMessage = "The value provided to --inputUrl must be a valid absolute URL.";
            options = null;
            return false;
        }

        options = new AppOptions(inputListUri, string.IsNullOrWhiteSpace(kmlFileValue) ? null : kmlFileValue, verbose, exportCsv);
        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Outputs usage information for the application to the console.
    /// </summary>
    public static void PrintUsage()
    {
        var executableName = Path.GetFileName(Environment.ProcessPath) ?? "GMapListToKml";
        Console.WriteLine($"Usage: {executableName} --inputUrl <url> [--kml <path>] [--csv] [--verbose]");
        Console.WriteLine();
        Console.WriteLine("Required arguments:");
        Console.WriteLine("  --inputUrl      The Google Maps list URL to download and convert into KML.");
        Console.WriteLine();
        Console.WriteLine("Optional arguments:");
        Console.WriteLine("  --kml           Path to the KML file to create. Defaults to the list name with a .kml extension.");
        Console.WriteLine("  --csv           Also export the list as a CSV file.");
        Console.WriteLine("  --verbose       Enables verbose logging for troubleshooting.");
        Console.WriteLine("  --help, -h      Displays this usage information.");
    }
}
