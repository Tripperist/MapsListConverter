using Tripperist.MapsListConverter.Core.Lists;

namespace Tripperist.MapsListConverter.Service.Export;

/// <summary>
/// Produces KML documents from <see cref="MapsList"/> instances.
/// </summary>
public interface IKmlExportService
{
    /// <summary>
    /// Writes the supplied list to the specified path.
    /// </summary>
    /// <param name="list">List to export.</param>
    /// <param name="filePath">Destination path.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    Task WriteAsync(MapsList list, string filePath, CancellationToken cancellationToken);
}
