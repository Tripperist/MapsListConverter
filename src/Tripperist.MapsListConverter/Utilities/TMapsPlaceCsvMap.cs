using System;
using System.Collections.Generic;
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

        Map(place => place.GooglePlaceDetails != null ? place.GooglePlaceDetails.PlaceId : null)
            .Name("GooglePlaceId");

        Map(place => place.GooglePlaceDetails != null ? place.GooglePlaceDetails.ResourceName : null)
            .Name("GoogleResourceName");

        Map(place => place.GooglePlaceDetails?.FormattedAddress ?? place.Address)
            .Name("GoogleFormattedAddress");

        Map(place => place.GooglePlaceDetails != null ? place.GooglePlaceDetails.ShortFormattedAddress : null)
            .Name("GoogleShortFormattedAddress");

        Map(place => place.GooglePlaceDetails != null ? place.GooglePlaceDetails.AddressDescriptor : null)
            .Name("GoogleAddressDescriptor");

        Map(place => place.GooglePlaceDetails != null ? place.GooglePlaceDetails.AdrFormatAddress : null)
            .Name("GoogleAdrFormatAddress");

        Map(place => place.GooglePlaceDetails?.Location?.Latitude ?? place.Latitude)
            .Name("GoogleLatitude");

        Map(place => place.GooglePlaceDetails?.Location?.Longitude ?? place.Longitude)
            .Name("GoogleLongitude");

        Map(place => place.GooglePlaceDetails != null ? FormatLocation(place.GooglePlaceDetails.Location) : null)
            .Name("GoogleLocation");

        Map(place => place.GooglePlaceDetails != null ? FormatViewport(place.GooglePlaceDetails.Viewport) : null)
            .Name("GoogleViewport");

        Map(place => place.GooglePlaceDetails != null ? JoinValues(place.GooglePlaceDetails.Types) : null)
            .Name("GoogleTypes");

        Map(place => place.GooglePlaceDetails != null ? JoinValues(place.GooglePlaceDetails.Attributions, " | ") : null)
            .Name("GoogleAttributions");
    }

    private static string? FormatLocation(GooglePlaceLocation? location)
    {
        if (location == null)
        {
            return null;
        }

        return string.Format(CultureInfo.InvariantCulture, "{0},{1}", location.Latitude, location.Longitude);
    }

    private static string? FormatViewport(GooglePlaceViewport? viewport)
    {
        if (viewport?.Low == null || viewport.High == null)
        {
            return null;
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "Low:{0},{1}|High:{2},{3}",
            viewport.Low.Latitude,
            viewport.Low.Longitude,
            viewport.High.Latitude,
            viewport.High.Longitude);
    }

    private static string? JoinValues(IReadOnlyList<string>? values, string separator = ";")
    {
        if (values is not { Count: > 0 })
        {
            return null;
        }

        return string.Join(separator, values);
    }
}
