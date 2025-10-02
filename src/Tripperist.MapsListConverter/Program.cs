using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tripperist.MapsListConverter.App.Hosting;
using Tripperist.MapsListConverter.App.IO;
using Tripperist.MapsListConverter.App.Localization;
using Tripperist.MapsListConverter.App.Validation;
using Tripperist.MapsListConverter.Console.Commands;
using Tripperist.MapsListConverter.Console.Options;
using Tripperist.MapsListConverter.Core.Configuration;
using Tripperist.MapsListConverter.Service.Export;
using Tripperist.MapsListConverter.Service.Places;
using Tripperist.MapsListConverter.Service.Scraping;

namespace Tripperist.MapsListConverter;

/// <summary>
/// Entry point for the Tripperist Maps List Converter console application. The program is intentionally
/// lightweight and only wires together dependency injection, configuration, and graceful shutdown logic
/// so the rest of the application can focus on the conversion workflow.
/// </summary>
public static class Program
{
    /// <summary>
    /// Application entry point. Configures the host, dependency container, and orchestrates the command
    /// dispatch lifecycle.
    /// </summary>
    /// <param name="args">Command-line arguments provided by the user.</param>
    /// <returns>Zero when the conversion succeeds, otherwise a non-zero exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        var verboseRequested = VerbosityDetector.IsVerbose(args);

        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        ConfigureLogging(builder.Logging, verboseRequested);
        ConfigureServices(builder.Services);

        await using var host = builder.Build();
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancellationHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancellationHandler;

        var resources = host.Services.GetRequiredService<ResourceCatalog>();
        var logger = host.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Tripperist.MapsListConverter.Program");
        logger.LogInformation(resources.Log("ApplicationStarting", CultureInfo.CurrentCulture));

        try
        {
            var dispatcher = host.Services.GetRequiredService<ICommandDispatcher>();
            return await dispatcher.DispatchAsync(args, cancellation.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, resources.Error("UnhandledException", CultureInfo.CurrentCulture));
            return 1;
        }
        finally
        {
            logger.LogInformation(resources.Log("ApplicationCompleted", CultureInfo.CurrentCulture));
            Console.CancelKeyPress -= cancellationHandler;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ResourceCatalog>();
        services.AddSingleton<IUsageWriter, ConsoleUsageWriter>();
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<CommandHandler<ExtractListOptions>, ExtractListCommandHandler>();
        services.AddSingleton<ICommandOptionsBinder<ExtractListOptions>, ExtractListOptionsBinder>();
        services.AddSingleton<IOptionsValidator<ExtractListOptions>, DataAnnotationsValidator<ExtractListOptions>>();
        services.AddSingleton<IFileNameSanitizer, FileNameSanitizer>();
        services.AddSingleton<IKmlExportService, KmlExportService>();
        services.AddSingleton<ICsvExportService, CsvExportService>();
        services.AddSingleton<IMapsListScraper, PlaywrightMapsListScraper>();
        services.AddHttpClient<IGooglePlacesClient, GooglePlacesClient>();

        services.AddOptions<GooglePlacesSettings>()
            .BindConfiguration(GooglePlacesSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void ConfigureLogging(ILoggingBuilder loggingBuilder, bool verbose)
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddConsole();
        loggingBuilder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
    }
}
