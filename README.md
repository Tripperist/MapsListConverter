# MapsListConverter

MapsListConverter is a .NET console utility that downloads a public Google Maps list and converts it into more portable formats such as KML and CSV. It is primarily intended for personal exports or archival of your own shared lists.

## Repository layout

The repository is intentionally small and organised around a single console project:

| Path | Description |
|------|-------------|
| `src/Tripperist.MapsListConverter` | The main application project. Contains the command line entry point, option parsing, scraping services, and file writers. |
| `src/Tripperist.MapsListConverter/Models` | Domain models used throughout the application. |
| `src/Tripperist.MapsListConverter/Options` | Command line option definitions and parsing logic. |
| `src/Tripperist.MapsListConverter/Services` | Application services such as the HTML scraper and the KML writer. |
| `src/Tripperist.MapsListConverter/Utilities` | Small helpers that do not fit elsewhere (e.g., resolving output paths). |

The project folder contains its own `README.md` with usage instructions specific to the console application.

## Getting started

1. Install the [.NET 10 SDK (RC or later)](https://dotnet.microsoft.com/).
2. Restore dependencies and build the application:

   ```bash
   dotnet build src/Tripperist.MapsListConverter/Tripperist.MapsListConverter.csproj
   ```

3. Run the converter, providing the URL of the shared Google Maps list you want to export:

   ```bash
   dotnet run --project src/Tripperist.MapsListConverter -- --inputList "https://maps.app.goo.gl/Example" --csv
   ```

The output KML file is written to the path you specify with `--outputFile`. When omitted, the tool generates a filename from the list name in the current directory. If `--csv` is supplied, a CSV file is written alongside the KML using the same base name.

For additional details and command line options see [`src/Tripperist.MapsListConverter/README.md`](src/Tripperist.MapsListConverter/README.md).
