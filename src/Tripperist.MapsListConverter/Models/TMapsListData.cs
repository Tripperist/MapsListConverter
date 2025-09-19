using System.Collections.Generic;

namespace Tripperist.MapsListConverter.Models;

/// <summary>
/// Represents the metadata and places returned from the Tripperist Maps list.
/// </summary>
/// <param name="Name">The title of the list as displayed by Google Maps.</param>
/// <param name="Description">Optional descriptive text provided by the list owner.</param>
/// <param name="Creator">The public display name of the person sharing the list.</param>
/// <param name="Places">Every place entry extracted from the initialization payload.</param>
public sealed record TMapsListData(
    string Name,
    string? Description,
    string? Creator,
    IReadOnlyList<TMapsPlace> Places);

