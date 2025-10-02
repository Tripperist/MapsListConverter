namespace Tripperist.MapsListConverter.App.IO;

/// <summary>
/// Sanitizes file names to ensure compatibility with the underlying file system.
/// </summary>
public interface IFileNameSanitizer
{
    /// <summary>
    /// Produces a safe file name from the provided text.
    /// </summary>
    /// <param name="name">Raw name that may contain invalid characters.</param>
    /// <param name="extension">Desired file extension including the leading dot.</param>
    /// <returns>Sanitized file name.</returns>
    string Sanitize(string name, string extension);
}
