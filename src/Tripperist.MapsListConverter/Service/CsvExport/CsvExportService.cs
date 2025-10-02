using System;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Tripperist.Core.GoogleMaps;
using Tripperist.Core.Localization;

namespace Tripperist.Service.CsvExport;

/// <summary>
/// Creates CSV exports using <see cref="CsvHelper"/>.
/// </summary>
public sealed class CsvExportService(ILogger<CsvExportService> logger) : ICsvExportService
{
    private readonly ILogger<CsvExportService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ResourceManager _logMessages = ResourceCatalog.LogMessages;

    /// <inheritdoc />
    public async Task WriteAsync(GoogleMapsList list, string csvPath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(list);
        if (string.IsNullOrWhiteSpace(csvPath))
        {
            throw new ArgumentException("CSV path must be provided.", nameof(csvPath));
        }

        _logger.LogInformation(_logMessages.GetString("CsvExportStarting"), csvPath);

        // We proactively create the directory to avoid runtime errors when users target nested folders.
        var directory = Path.GetDirectoryName(csvPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(csvPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(stream);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.WriteRecordsAsync(list.Places, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);

        _logger.LogInformation(_logMessages.GetString("CsvExportCompleted"), csvPath);
    }
}
