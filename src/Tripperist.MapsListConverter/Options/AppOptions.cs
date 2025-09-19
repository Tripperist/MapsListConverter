using System;

namespace Tripperist.MapsListConverter.Options;

/// <summary>
/// Represents validated arguments supplied to the application at start-up.
/// </summary>
public sealed class AppOptions
{
    /// <summary>
    /// Gets the Google Maps list URL that should be downloaded.
    /// </summary>
    public Uri InputListUri { get; }

    /// <summary>
    /// Gets the optional location where the generated KML file should be written.
    /// </summary>
    public string? OutputFilePath { get; }

    /// <summary>
    /// Gets a value indicating whether verbose logging is enabled.
    /// </summary>
    public bool Verbose { get; }

    /// <summary>
    /// Gets a value indicating whether a CSV file should also be generated.
    /// </summary>
    public bool Csv { get; }

    public AppOptions(Uri inputListUri, string? outputFilePath, bool verbose, bool csv)
    {
        InputListUri = inputListUri;
        OutputFilePath = outputFilePath;
        Verbose = verbose;
        Csv = csv;
    }
}
