using System.Collections.Generic;

namespace Tripperist.MapsListConverter.Core.Lists;

/// <summary>
/// Describes the aggregate data for a single Google Maps saved list.
/// </summary>
public sealed class MapsList
{
    /// <summary>
    /// Gets or sets the list name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the list creator's display name.
    /// </summary>
    public string? Creator { get; set; }

    /// <summary>
    /// Gets or sets the places associated with the list.
    /// </summary>
    public IList<MapsPlace> Places { get; set; } = new List<MapsPlace>();
}
