# Tripperist.MapsListConverter

Tripperist.MapsListConverter is a .NET 10 console application that downloads one of your public Google Maps lists and exports it as a Keyhole Markup Language (KML) document. The tool focuses on the data embedded inside the `window.APP_INITIALIZATION_STATE` script block, which contains the list name, description, creator, and every place entry.

## Requirements

* [.NET 10 SDK (Release Candidate or later)](https://dotnet.microsoft.com/)
* Network access to reach `maps.app.goo.gl` and `google.com`

## Usage

```bash
TripperistMapsListConverter --inputList "{URL of Shared List}" 

### Arguments

| Argument              | Required | Description                                                                 |
|-----------------------|----------|-----------------------------------------------------------------------------|
| `--inputList <url>`   | Yes      | Google Maps list URL to download and convert into KML.                      |
| `--outputFile <path>` | No       | Path to the KML file to create. Defaults to the list name with a .kml extension. |
| `--csv`               | No       | Also export the list as a CSV file.                                         |
| `--verbose`           | No       | Enables verbose logging for troubleshooting.                                |
| `--help`, `-h`        | No       | Displays usage information.                                                 |

### Example

```bash
Tripperist.MapsListConverter --inputList "https://maps.app.goo.gl/Dr5BWZN1Z1RL2fu3A" --verbose
```

The command above will create a file named `2023.03.21.MSYDelta.kml` (based on the list name) in the current directory unless `--outputFile` is specified. If `--csv` is provided, a CSV file with the same base name will also be created.

## Notes

* The scraper relies on HtmlAgilityPack so it can robustly locate the initialization script, even if Google changes HTML whitespace or formatting.
* The generated KML file stores address and notes inside the placemark description. Latitude and longitude are added whenever Google exposes them in the payload.
