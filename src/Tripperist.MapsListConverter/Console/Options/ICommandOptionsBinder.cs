namespace Tripperist.MapsListConverter.Console.Options;

/// <summary>
/// Binds command-line arguments to a strongly typed options object.
/// </summary>
/// <typeparam name="TOptions">Options type.</typeparam>
public interface ICommandOptionsBinder<out TOptions>
{
    /// <summary>
    /// Binds the command-line arguments to <typeparamref name="TOptions"/>.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Bound options instance.</returns>
    TOptions Bind(string[] args);
}
