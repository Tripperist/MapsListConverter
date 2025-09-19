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

    public TMapsPlace(string name, string? address, string? notes, double? latitude, double? longitude)
    {
        Name = name;
        Address = address;
        Notes = notes;
        Latitude = latitude;
        Longitude = longitude;
    }
}
