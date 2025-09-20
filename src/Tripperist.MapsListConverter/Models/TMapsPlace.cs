namespace Tripperist.MapsListConverter.Models;

/// <summary>
/// Represents a place in a Tripperist Maps list.
/// </summary>
public class TMapsPlace
{
    /// <summary>
    /// Display name of the place.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Optional address provided by Google Maps for the place.
    /// </summary>
    public string? Address { get; }

    /// <summary>
    /// Any notes authored by the list owner.
    /// </summary>
    public string? Notes { get; }

    /// <summary>
    /// Latitude in decimal degrees when supplied by Google Maps.
    /// </summary>
    public double? Latitude { get; }

    /// <summary>
    /// Longitude in decimal degrees when supplied by Google Maps.
    /// </summary>
    public double? Longitude { get; }

    /// <summary>
    /// Average rating reported by Google Maps when available.
    /// </summary>
    public double? Rating { get; }

    /// <summary>
    /// Total number of reviews associated with the place when available.
    /// </summary>
    public int? ReviewCount { get; }

    /// <summary>
    /// Publicly listed phone number.
    /// </summary>
    public string? Phone { get; }

    /// <summary>
    /// Official website URL provided in the Google Maps listing.
    /// </summary>
    public string? Website { get; }

    /// <summary>
    /// Opening hours summary text shown by Google Maps.
    /// </summary>
    public string? OpeningHours { get; }

    /// <summary>
    /// Plus code (open location code) when Google exposes it.
    /// </summary>
    public string? PlusCode { get; }

    public TMapsPlace(
        string name,
        string? address,
        string? notes,
        double? latitude,
        double? longitude,
        double? rating = null,
        int? reviewCount = null,
        string? phone = null,
        string? website = null,
        string? openingHours = null,
        string? plusCode = null)
    {
        Name = name;
        Address = address;
        Notes = notes;
        Latitude = latitude;
        Longitude = longitude;
        Rating = rating;
        ReviewCount = reviewCount;
        Phone = phone;
        Website = website;
        OpeningHours = openingHours;
        PlusCode = plusCode;
    }
}
