using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;
using Tripperist.MapsListConverter.App.IO;
using Tripperist.MapsListConverter.App.Localization;
using Tripperist.MapsListConverter.Console.Options;
using Tripperist.MapsListConverter.Core.Lists;
using Tripperist.MapsListConverter.Service.Export;
using Tripperist.MapsListConverter.Service.Places;
using Tripperist.MapsListConverter.Service.Scraping;

namespace Tripperist.MapsListConverter.Console.Commands;

/// <summary>
/// Primary command handler responsible for orchestrating scraping, API enrichment, and file export.
/// </summary>
public sealed class ExtractListCommandHandler(
    IMapsListScraper scraper,
    IGooglePlacesClient placesClient,
    IKmlExportService kmlExportService,
    ICsvExportService csvExportService,
    IFileNameSanitizer fileNameSanitizer,
    ResourceCatalog resources,
    ILogger<ExtractListCommandHandler> logger) : CommandHandler<ExtractListOptions>(logger)
{
    private readonly IMapsListScraper _scraper = scraper ?? throw new ArgumentNullException(nameof(scraper));
    private readonly IGooglePlacesClient _placesClient = placesClient ?? throw new ArgumentNullException(nameof(placesClient));
    private readonly IKmlExportService _kmlExportService = kmlExportService ?? throw new ArgumentNullException(nameof(kmlExportService));
    private readonly ICsvExportService _csvExportService = csvExportService ?? throw new ArgumentNullException(nameof(csvExportService));
    private readonly IFileNameSanitizer _fileNameSanitizer = fileNameSanitizer ?? throw new ArgumentNullException(nameof(fileNameSanitizer));
    private readonly ResourceCatalog _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    private readonly ILogger<ExtractListCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    protected override async Task<int> ExecuteCoreAsync(ExtractListOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        var listUri = options.InputUrl ?? throw new InvalidOperationException("Input URL is required.");

        var mapsList = await _scraper.LoadAsync(listUri, cancellationToken).ConfigureAwait(false);
        await EnrichWithPlaceIdsAsync(mapsList, cancellationToken).ConfigureAwait(false);

        var kmlPath = string.IsNullOrWhiteSpace(options.KmlOutputPath)
            ? _fileNameSanitizer.Sanitize(mapsList.Name, ".kml")
            : options.KmlOutputPath;

        _logger.LogInformation(_resources.Log("WritingKml", CultureInfo.CurrentCulture), kmlPath);
        await _kmlExportService.WriteAsync(mapsList, kmlPath, cancellationToken).ConfigureAwait(false);

        if (options.ExportCsv)
        {
            var csvPath = Path.ChangeExtension(kmlPath, ".csv") ?? kmlPath + ".csv";
            _logger.LogInformation(_resources.Log("WritingCsv", CultureInfo.CurrentCulture), csvPath);
            await _csvExportService.WriteAsync(mapsList, csvPath, cancellationToken).ConfigureAwait(false);
        }

        return 0;
    }

    private async Task EnrichWithPlaceIdsAsync(MapsList list, CancellationToken cancellationToken)
    {
        foreach (var place in list.Places)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogDebug(_resources.Log("PlacesApiLookup", CultureInfo.CurrentCulture), place.Name);

            try
            {
                place.PlaceId = await _placesClient.ResolvePlaceIdAsync(place.Name, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve Place ID for {PlaceName}", place.Name);
            }
        }
    }
}
