using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Tripperist.MapsListConverter.Core.Lists;

namespace Tripperist.MapsListConverter.Service.Export;

/// <summary>
/// Handles CSV serialization for maps lists using CsvHelper to ensure robust formatting.
/// </summary>
public sealed class CsvExportService(ILogger<CsvExportService> logger) : ICsvExportService
{
    private readonly ILogger<CsvExportService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task WriteAsync(MapsList list, string filePath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(filePath);

        cancellationToken.ThrowIfCancellationRequested();

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(stream);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.WriteRecordsAsync(list.Places, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);

        _logger.LogInformation("CSV export complete: {FilePath}", filePath);
    }
}
