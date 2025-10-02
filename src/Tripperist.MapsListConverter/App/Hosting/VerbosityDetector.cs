using System.Globalization;

namespace Tripperist.MapsListConverter.App.Hosting;

/// <summary>
/// Provides a lightweight mechanism for detecting the <c>--verbose</c> switch prior to the dependency
/// injection container being constructed. This enables the application to configure logging levels early
/// in the startup pipeline without duplicating argument parsing later.
/// </summary>
public static class VerbosityDetector
{
    /// <summary>
    /// Determines whether the command-line arguments contain the <c>--verbose</c> option.
    /// </summary>
    /// <param name="args">Command-line arguments provided to the application.</param>
    /// <returns><see langword="true"/> when verbose output is requested; otherwise, <see langword="false"/>.</returns>
    public static bool IsVerbose(string[] args)
    {
        if (args is null)
        {
            return false;
        }

        foreach (var argument in args)
        {
            if (string.Equals(argument, "--verbose", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
