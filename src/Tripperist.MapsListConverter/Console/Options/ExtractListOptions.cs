using System.ComponentModel.DataAnnotations;
using Tripperist.MapsListConverter.App.Validation;

namespace Tripperist.MapsListConverter.Console.Options;

/// <summary>
/// Command-line options for exporting a Google Maps saved list.
/// </summary>
public sealed class ExtractListOptions
{
    /// <summary>
    /// Gets or sets the input Google Maps list URL.
    /// </summary>
    [Required]
    public Uri? InputUrl { get; init; }

    /// <summary>
    /// Gets or sets the location of the KML file to be produced.
    /// </summary>
    [Required]
    [NotEmptyOrWhitespace]
    public string KmlOutputPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a CSV export should also be generated. When enabled the CSV
    /// file shares the same base name as the KML output.
    /// </summary>
    public bool ExportCsv { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether verbose logging is enabled.
    /// </summary>
    public bool Verbose { get; init; }
}
