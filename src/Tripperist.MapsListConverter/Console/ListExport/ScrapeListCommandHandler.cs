using System;
using System.IO;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tripperist.Console.ListExport.Options;
using Tripperist.Core.Commands;
using Tripperist.Core.IO;
using Tripperist.Core.Localization;
using Tripperist.Service.CsvExport;
using Tripperist.Service.GoogleMaps;
using Tripperist.Service.GooglePlaces;
using Tripperist.Service.KmlExport;

namespace Tripperist.Console.ListExport;

/// <summary>
/// Coordinates scraping, enrichment and export into the requested file formats.
/// </summary>
public sealed class ScrapeListCommandHandler(
    IListScrapingService listScrapingService,
    IPlacesEnrichmentService enrichmentService,
    KmlWriter kmlWriter,
    ICsvExportService csvExportService,
    ILogger<ScrapeListCommandHandler> logger) : CommandHandler<AppOptions>
{
    private readonly IListScrapingService _listScrapingService = listScrapingService ?? throw new ArgumentNullException(nameof(listScrapingService));
    private readonly IPlacesEnrichmentService _enrichmentService = enrichmentService ?? throw new ArgumentNullException(nameof(enrichmentService));
    private readonly KmlWriter _kmlWriter = kmlWriter ?? throw new ArgumentNullException(nameof(kmlWriter));
    private readonly ICsvExportService _csvExportService = csvExportService ?? throw new ArgumentNullException(nameof(csvExportService));
    private readonly ILogger<ScrapeListCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ResourceManager _logMessages = ResourceCatalog.LogMessages;

    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(AppOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        // Scraping and enrichment are intentionally separate steps so we can better understand which phase fails when errors occur.
        var scrapedList = await _listScrapingService.ScrapeAsync(options.InputListUri, options.Verbose, cancellationToken).ConfigureAwait(false);
        var enrichedList = await _enrichmentService.EnrichAsync(scrapedList, cancellationToken).ConfigureAwait(false);

        var kmlPath = OutputPathResolver.Resolve(options.KmlFilePath, enrichedList.Name);
        await _kmlWriter.WriteAsync(enrichedList, kmlPath, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(_logMessages.GetString("KmlExportCompleted"), kmlPath);

        if (options.GenerateCsv)
        {
            var csvPath = Path.ChangeExtension(kmlPath, ".csv") ?? kmlPath + ".csv";
            await _csvExportService.WriteAsync(enrichedList, csvPath, cancellationToken).ConfigureAwait(false);
        }

        return 0;
    }
}
