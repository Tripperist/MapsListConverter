# MapsListConverter

MapsListConverter is a .NET console utility that downloads public Google Maps lists and exports them to KML or CSV so they can
be archived or imported into other tooling. The code is organised as a single solution with a focus on testable, well-documented
services for scraping the source HTML and emitting geospatial files.

## Repository layout

| Path | Description |
|------|-------------|
| `src/Tripperist.MapsListConverter` | Main console application, including the entry point, command-line parsing, scraping services, and output utilities. |
| `src/Tripperist.MapsListConverter/README.md` | Detailed usage instructions for the converter, including supported command-line arguments. |
| `LICENSE` | Licensing information for this repository. |

Within the application project the code is organised by responsibility:

* `Options/` contains command-line options and parsing logic.
* `Models/` holds the domain models for Google Maps list metadata and places.
* `Services/` provides the HTML scraping pipeline and the KML writer.
* `Utilities/` offers small helpers such as output path management.

## Getting started

1. Install the [.NET 10 SDK (Release Candidate or later)](https://dotnet.microsoft.com/).
2. Restore dependencies and build the application:

   ```bash
   dotnet build src/Tripperist.MapsListConverter/Tripperist.MapsListConverter.csproj
   ```

3. Run the converter from the project directory, specifying the list URL and optional arguments:

   ```bash
   dotnet run --project src/Tripperist.MapsListConverter -- --inputList "<public Google Maps list URL>" --csv
   ```

Consult the project-level [README](src/Tripperist.MapsListConverter/README.md) for the full set of arguments and workflow
examples.
