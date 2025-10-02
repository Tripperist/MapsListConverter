using System.Text;

namespace Tripperist.MapsListConverter.App.IO;

/// <summary>
/// Provides a deterministic file-name sanitization strategy to keep exports cross-platform compatible.
/// </summary>
public sealed class FileNameSanitizer : IFileNameSanitizer
{
    /// <inheritdoc />
    public string Sanitize(string name, string extension)
    {
        var safeName = string.IsNullOrWhiteSpace(name) ? "export" : name.Trim();
        var builder = new StringBuilder(safeName.Length);
        foreach (var character in safeName)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        return builder.ToString() + extension;
    }
}
