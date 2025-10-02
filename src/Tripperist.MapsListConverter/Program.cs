using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using Tripperist.MapsListConverter.Models;
using Tripperist.MapsListConverter.Options;
using Tripperist.MapsListConverter.Services;
using Tripperist.MapsListConverter.Services.GooglePlaces;
using Tripperist.MapsListConverter.Utilities;
using Microsoft.Extensions.Logging;

namespace Tripperist.MapsListConverter;

/// <summary>
/// Entry point of the application. Responsible for orchestrating the overall execution flow and
/// wiring up infrastructure such as logging and HTTP dependencies.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main method that coordinates argument parsing, data retrieval, and KML file generation.
    /// </summary>
    /// <param name="args">Command line arguments provided by the user.</param>
    /// <returns>Zero when the application finishes successfully, otherwise a non-zero error code.</returns>
    public static async Task<int> Main(string[] args)
    {
        // A dedicated help flag is easier for users than throwing an error, so we short-circuit early.
        if (AppOptionsParser.ShouldShowHelp(args))
        {
            AppOptionsParser.PrintUsage();
            return 0;
        }

        if (!AppOptionsParser.TryParse(args, out var options, out var errorMessage))
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                Console.Error.WriteLine(errorMessage);
            }

            AppOptionsParser.PrintUsage();
            return 1;
        }

        // We immediately store the parsed options in a non-nullable variable so the remainder of the method can use it safely.
        var appOptions = options ?? throw new InvalidOperationException("Options parsing returned a null result.");

        // The logger factory is built once so every service shares the same formatting and level configuration.
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                options.SingleLine = true;
            });
            builder.SetMinimumLevel(appOptions.Verbose ? LogLevel.Debug : LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger(typeof(Program));

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler? cancelHandler = null;
        cancelHandler = (_, eventArgs) =>
        {
            // Cancelling prevents the process from terminating abruptly, giving us time to clean up resources.
            eventArgs.Cancel = true;
            if (!cancellation.IsCancellationRequested)
            {
                logger.LogWarning("Cancellation requested. Attempting to stop gracefully...");
                cancellation.Cancel();
            }
        };

        Console.CancelKeyPress += cancelHandler;

        try
        {
            using var httpClient = CreateHttpClient();

            // Use parsed option for CSV export
            var exportCsv = appOptions.Csv;

            var scraper = new TMapsListScraper(httpClient, loggerFactory.CreateLogger<TMapsListScraper>());
            var listData = await scraper.FetchListAsync(appOptions.InputListUri, cancellation.Token).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(appOptions.GooglePlacesApiKey))
            {
                using var placesHttpClient = CreateGooglePlacesHttpClient();
                var placesClient = new GooglePlacesClient(
                    placesHttpClient,
                    loggerFactory.CreateLogger<GooglePlacesClient>(),
                    appOptions.GooglePlacesApiKey);

                var placesEnricher = new GooglePlacesEnricher(
                    placesClient,
                    loggerFactory.CreateLogger<GooglePlacesEnricher>());

                listData = await placesEnricher.EnrichAsync(listData, cancellation.Token).ConfigureAwait(false);
            }
            else
            {
                logger.LogInformation("Google Places API key not provided. Skipping Places enrichment step.");
            }

            var outputPath = OutputPathResolver.Resolve(appOptions.OutputFilePath, listData.Name);

            var kmlWriter = new KmlWriter(loggerFactory.CreateLogger<KmlWriter>());
            await kmlWriter.WriteAsync(listData, outputPath, cancellation.Token).ConfigureAwait(false);

            logger.LogInformation("KML file created at {OutputPath}", outputPath);

            if (exportCsv)
            {
                cancellation.Token.ThrowIfCancellationRequested();

                var csvFilePath = Path.ChangeExtension(outputPath, ".csv") ?? outputPath + ".csv";

                await using var csvStream = new FileStream(csvFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var streamWriter = new StreamWriter(csvStream);
                using var csv = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<TMapsPlaceCsvMap>();
                await csv.WriteRecordsAsync(listData.Places, cancellation.Token).ConfigureAwait(false);

                logger.LogInformation("CSV file created at {CsvFilePath}", csvFilePath);
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Operation cancelled by user.");
            return 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while generating the KML file.");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    /// <summary>
    /// Creates and configures an <see cref="HttpClient"/> instance tailored for Tripperist Maps requests.
    /// </summary>
    private static HttpClient CreateHttpClient()
    {
        // We mimic a standard browser so Tripperist returns the same HTML a user would see in practice.
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(45)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

        return client;
    }

    /// <summary>
    /// Creates and configures an <see cref="HttpClient"/> instance for Google Places requests.
    /// </summary>
    private static HttpClient CreateGooglePlacesHttpClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://places.googleapis.com/"),
            Timeout = TimeSpan.FromSeconds(30)
        };

        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        return client;
    }
}
