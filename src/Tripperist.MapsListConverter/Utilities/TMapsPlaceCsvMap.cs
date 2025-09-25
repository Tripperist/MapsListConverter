using System;
using System.Globalization;
using CsvHelper.Configuration;
using Tripperist.MapsListConverter.Models;

namespace Tripperist.MapsListConverter.Utilities;

/// <summary>
/// CSV mapping configuration for <see cref="TMapsPlace"/> values.
/// </summary>
public sealed class TMapsPlaceCsvMap : ClassMap<TMapsPlace>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TMapsPlaceCsvMap"/> class.
    /// </summary>
    public TMapsPlaceCsvMap()
    {
        Map(place => place.Name);
        Map(place => place.Address);
        Map(place => place.Notes);
        Map(place => place.Latitude);
        Map(place => place.Longitude);

        Map()
            .Name("GooglePlaceId")
            .Convert(args => args.Value.GooglePlaceDetails?.PlaceId);

        Map()
            .Name("GoogleResourceName")
            .Convert(args => args.Value.GooglePlaceDetails?.ResourceName);

        Map()
            .Name("GoogleFormattedAddress")
            .Convert(args => args.Value.GooglePlaceDetails?.FormattedAddress ?? args.Value.Address);

        Map()
            .Name("GoogleShortFormattedAddress")
            .Convert(args => args.Value.GooglePlaceDetails?.ShortFormattedAddress);

        Map()
            .Name("GoogleAddressDescriptor")
            .Convert(args => args.Value.GooglePlaceDetails?.AddressDescriptor);

        Map()
            .Name("GoogleAdrFormatAddress")
            .Convert(args => args.Value.GooglePlaceDetails?.AdrFormatAddress);

        Map()
            .Name("GoogleLatitude")
            .Convert(args => args.Value.GooglePlaceDetails?.Location?.Latitude ?? args.Value.Latitude);

        Map()
            .Name("GoogleLongitude")
            .Convert(args => args.Value.GooglePlaceDetails?.Location?.Longitude ?? args.Value.Longitude);

        Map()
            .Name("GoogleLocation")
            .Convert(args => args.Value.GooglePlaceDetails?.Location is { } location
                ? string.Format(CultureInfo.InvariantCulture, "{0},{1}", location.Latitude, location.Longitude)
                : null);

        Map()
            .Name("GoogleViewport")
            .Convert(args => args.Value.GooglePlaceDetails?.Viewport is { } viewport
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "Low:{0},{1}|High:{2},{3}",
                    viewport.Low.Latitude,
                    viewport.Low.Longitude,
                    viewport.High.Latitude,
                    viewport.High.Longitude)
                : null);

        Map()
            .Name("GoogleTypes")
            .Convert(args => args.Value.GooglePlaceDetails?.Types is { Count: > 0 } types
                ? string.Join(';', types)
                : null);

        Map()
            .Name("GoogleAttributions")
            .Convert(args => args.Value.GooglePlaceDetails?.Attributions is { Count: > 0 } attributions
                ? string.Join(" | ", attributions)
                : null);
    }
}
