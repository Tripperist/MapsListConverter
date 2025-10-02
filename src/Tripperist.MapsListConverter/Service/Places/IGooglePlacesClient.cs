namespace Tripperist.MapsListConverter.Service.Places;

/// <summary>
/// Contract for resolving Google Places metadata.
/// </summary>
public interface IGooglePlacesClient
{
    /// <summary>
    /// Resolves the Google Places identifier for the specified place name.
    /// </summary>
    /// <param name="placeName">Human readable place name.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>The Google Place ID when found; otherwise, <see langword="null"/>.</returns>
    Task<string?> ResolvePlaceIdAsync(string placeName, CancellationToken cancellationToken);
}
