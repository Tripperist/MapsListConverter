using System.Threading;
using System.Threading.Tasks;
using Tripperist.Core.GoogleMaps;

namespace Tripperist.Service.CsvExport;

/// <summary>
/// Persists list data as a CSV document.
/// </summary>
public interface ICsvExportService
{
    /// <summary>
    /// Writes the supplied list to the specified CSV path.
    /// </summary>
    Task WriteAsync(GoogleMapsList list, string csvPath, CancellationToken cancellationToken);
}
