namespace Tripperist.MapsListConverter.Console.Commands;

using System;

/// <summary>
/// Emits usage instructions to standard output. Keeping this logic isolated facilitates localization later.
/// </summary>
public sealed class ConsoleUsageWriter : IUsageWriter
{
    /// <inheritdoc />
    public void Write()
    {
        Console.WriteLine("Usage: mapslistconverter --input <url> --kml <path> [--csv] [--verbose]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --input <url>   The Google Maps saved list URL to export.");
        Console.WriteLine("  --kml <path>    Destination file path for the generated KML document.");
        Console.WriteLine("  --csv           Emits a CSV file alongside the KML output.");
        Console.WriteLine("  --verbose       Enables verbose diagnostic logging.");
        Console.WriteLine("  --help          Displays this message.");
    }
}
