using System;

namespace Tripperist.MapsListConverter.Options;

/// <summary>
/// Strongly typed representation of the command line options accepted by the application.
/// </summary>
public sealed class AppOptions
{
    /// <summary>
    /// The publicly shared Google Maps list that should be downloaded.
    /// </summary>
    public Uri InputListUri { get; }

    /// <summary>
    /// Optional path to the KML file to create. If omitted a filename is generated from the list name.
    /// </summary>
    public string? OutputFilePath { get; }

    /// <summary>
    /// Enables verbose logging to help diagnose issues while scraping.
    /// </summary>
    public bool Verbose { get; }

    /// <summary>
    /// Indicates whether a CSV export should be produced alongside the KML file.
    /// </summary>
    public bool Csv { get; }

    /// <summary>
    /// Enables the richer, browser-driven scraping mode which gathers additional metadata.
    /// </summary>
    public bool Detailed { get; }

    public AppOptions(Uri inputListUri, string? outputFilePath, bool verbose, bool csv, bool detailed)
    {
        InputListUri = inputListUri ?? throw new ArgumentNullException(nameof(inputListUri));
        OutputFilePath = outputFilePath;
        Verbose = verbose;
        Csv = csv;
        Detailed = detailed;
    }
}
