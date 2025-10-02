namespace Tripperist.MapsListConverter.Console.Commands;

/// <summary>
/// Writes user-facing usage instructions.
/// </summary>
public interface IUsageWriter
{
    /// <summary>
    /// Writes usage information to the console.
    /// </summary>
    void Write();
}
