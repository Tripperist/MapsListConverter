using Tripperist.MapsListConverter.Core.Lists;

namespace Tripperist.MapsListConverter.Service.Export;

/// <summary>
/// Serializes <see cref="MapsList"/> instances to CSV format.
/// </summary>
public interface ICsvExportService
{
    /// <summary>
    /// Writes the CSV representation of the list to <paramref name="filePath"/>.
    /// </summary>
    /// <param name="list">List to export.</param>
    /// <param name="filePath">Destination path.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    Task WriteAsync(MapsList list, string filePath, CancellationToken cancellationToken);
}
