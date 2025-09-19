namespace Tripperist.MapsListConverter.Models;

/// <summary>
/// Represents a place in a Tripperist Maps list.
/// </summary>
public class TMapsPlace
{
    public string Name { get; }
    public string? Address { get; }
    public string? Notes { get; }
    public double? Latitude { get; }
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
