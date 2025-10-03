using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Options;
using Tripperist.Console.ListExport;
using Tripperist.Console.ListExport.Options;
using Tripperist.Core.Commands;
using Tripperist.Core.Configuration;
using Tripperist.Service.CsvExport;
using Tripperist.Service.GoogleMaps;
using Tripperist.Service.GooglePlaces;
using Tripperist.Service.KmlExport;

namespace Tripperist.Console.ListExport;

/// <summary>
/// Entry point responsible for bootstrapping dependency injection, logging, configuration and command execution.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main method that orchestrates argument parsing, service configuration and command execution.
    /// </summary>
    /// <param name="args">Command line arguments provided by the user.</param>
    /// <returns>Zero when the command succeeds, otherwise a non-zero exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        if (AppOptionsParser.ShouldShowHelp(args))
        {
            AppOptionsParser.PrintUsage();
            return 0;
        }

        if (!AppOptionsParser.TryParse(args, out var options, out var errorMessage))
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                System.Console.Error.WriteLine(errorMessage);
            }

            AppOptionsParser.PrintUsage();
            return 1;
        }

        var appOptions = options ?? throw new InvalidOperationException("Options parsing returned a null result.");

        using var cancellationSource = new CancellationTokenSource();
        ConsoleCancelEventHandler? cancelHandler = null;
        // Translating Ctrl+C into cooperative cancellation keeps the exporting pipeline in a consistent state.
        cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            if (!cancellationSource.IsCancellationRequested)
            {
                cancellationSource.Cancel();
            }
        };

        System.Console.CancelKeyPress += cancelHandler;

        try
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables(prefix: "TRIPPERIST_");

            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                options.SingleLine = true;
            });
            builder.Logging.SetMinimumLevel(appOptions.Verbose ? LogLevel.Debug : LogLevel.Information);

            builder.Services
                .AddOptions<GooglePlacesOptions>()
                .Bind(builder.Configuration.GetSection(GooglePlacesOptions.SectionName))
                .ValidateOnStart();

            builder.Services.AddSingleton<IPlaywrightFactory, PlaywrightFactory>();
            builder.Services.AddSingleton<IListScrapingService, PlaywrightListScrapingService>();
            builder.Services.AddSingleton<IPlacesEnrichmentService, PlacesEnrichmentService>();
            builder.Services.AddSingleton<ICsvExportService, CsvExportService>();
            builder.Services.AddSingleton<KmlWriter>();
            builder.Services.AddSingleton<CommandHandler<AppOptions>, ScrapeListCommandHandler>();

            builder.Services.AddHttpClient<IGooglePlacesClient, GooglePlacesClient>(client =>
            {
                client.BaseAddress = new Uri("https://maps.googleapis.com/maps/api/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            using var host = builder.Build();
            using var scope = host.Services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<CommandHandler<AppOptions>>();
            return await handler.ExecuteAsync(appOptions, cancellationSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return 1;
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
            if (appOptions.Verbose)
            {
                System.Console.Error.WriteLine(ex);
            }

            return 1;
        }
        finally
        {
            System.Console.CancelKeyPress -= cancelHandler;
        }
    }
}
