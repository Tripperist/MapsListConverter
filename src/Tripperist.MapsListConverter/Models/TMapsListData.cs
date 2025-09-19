using System.Collections.Generic;

namespace Tripperist.MapsListConverter.Models;

/// <summary>
/// Represents the metadata and places returned from the Tripperist Maps list.
/// </summary>
public sealed record TMapsListData(
    string Name,
    string? Description,
    string? Creator,
    IReadOnlyList<TMapsPlace> Places);
