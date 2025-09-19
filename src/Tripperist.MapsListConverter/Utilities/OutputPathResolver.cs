using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Tripperist.MapsListConverter.Utilities;

/// <summary>
/// Converts user input into a safe and absolute file path for the generated KML document.
/// </summary>
public static class OutputPathResolver
{
    /// <summary>
    /// Returns the absolute path for the KML file. When no explicit path is provided, the list name is used.
    /// </summary>
    public static string Resolve(string? requestedPath, string listName)
    {
        if (!string.IsNullOrWhiteSpace(requestedPath))
        {
            // We always work with absolute paths to make log statements and error messages unambiguous.
            return Path.GetFullPath(requestedPath);
        }

        var sanitizedName = SanitizeFileName(listName);
        var fileName = sanitizedName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase)
            ? sanitizedName
            : sanitizedName + ".kml";

        return Path.GetFullPath(fileName);
    }

    /// <summary>
    /// Creates a file path for an additional artifact that should live next to the KML output.
    /// </summary>
    /// <param name="primaryPath">The absolute path returned by <see cref="Resolve"/>.</param>
    /// <param name="newExtension">The extension for the companion file (with or without a leading dot).</param>
    public static string ResolveCompanionPath(string primaryPath, string newExtension)
    {
        if (string.IsNullOrWhiteSpace(primaryPath))
        {
            throw new ArgumentException("Primary path must be provided.", nameof(primaryPath));
        }

        if (string.IsNullOrWhiteSpace(newExtension))
        {
            throw new ArgumentException("New extension must be provided.", nameof(newExtension));
        }

        var normalizedExtension = newExtension.StartsWith('.')
            ? newExtension
            : "." + newExtension.TrimStart('.');

        return Path.ChangeExtension(primaryPath, normalizedExtension);
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "GoogleMapsList";
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(name.Length);

        foreach (var character in name)
        {
            builder.Append(invalidCharacters.Contains(character) || char.IsControl(character) ? '_' : character);
        }

        var sanitized = builder.ToString().Trim('_', ' ');
        return string.IsNullOrEmpty(sanitized) ? "GoogleMapsList" : sanitized;
    }
}
