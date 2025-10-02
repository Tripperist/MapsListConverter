using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Tripperist.MapsListConverter.Core.Lists;

namespace Tripperist.MapsListConverter.Service.Export;

/// <summary>
/// Generates KML documents that capture list metadata and individual place notes.
/// </summary>
public sealed class KmlExportService(ILogger<KmlExportService> logger) : IKmlExportService
{
    private readonly ILogger<KmlExportService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task WriteAsync(MapsList list, string filePath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(filePath);

        cancellationToken.ThrowIfCancellationRequested();

        var document = BuildDocument(list);
        var settings = new XmlWriterSettings
        {
            Async = true,
            Indent = true,
            Encoding = Encoding.UTF8
        };

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using var writer = XmlWriter.Create(stream, settings);
        document.WriteTo(writer);
        await writer.FlushAsync().ConfigureAwait(false);

        _logger.LogInformation("KML export complete: {FilePath}", filePath);
    }

    private static XDocument BuildDocument(MapsList list)
    {
        XNamespace ns = "http://www.opengis.net/kml/2.2";
        var placemarks = list.Places.Select(BuildPlacemark);

        return new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(ns + "kml",
                new XElement(ns + "Document",
                    new XElement(ns + "name", list.Name),
                    new XElement(ns + "description", list.Description ?? string.Empty),
                    placemarks)));
    }

    private static XElement BuildPlacemark(MapsPlace place)
    {
        XNamespace ns = "http://www.opengis.net/kml/2.2";
        var descriptionBuilder = new StringBuilder();
        if (place.Rating is not null)
        {
            descriptionBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Rating: {0:F1}", place.Rating));
        }

        if (place.ReviewCount is not null)
        {
            descriptionBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Reviews: {0}", place.ReviewCount));
        }

        if (!string.IsNullOrWhiteSpace(place.Note))
        {
            descriptionBuilder.AppendLine(place.Note);
        }

        if (!string.IsNullOrWhiteSpace(place.PlaceId))
        {
            descriptionBuilder.AppendLine($"Place ID: {place.PlaceId}");
        }

        if (!string.IsNullOrWhiteSpace(place.ImageUrl))
        {
            descriptionBuilder.AppendLine($"Image: {place.ImageUrl}");
        }

        return new XElement(ns + "Placemark",
            new XElement(ns + "name", place.Name),
            new XElement(ns + "description", descriptionBuilder.ToString().Trim()));
    }
}
