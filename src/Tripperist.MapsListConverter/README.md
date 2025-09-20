# Tripperist.MapsListConverter

Tripperist.MapsListConverter is a .NET 10 console application that downloads one of your public Google Maps lists and exports it as a Keyhole Markup Language (KML) document. The tool focuses on the data embedded inside the `window.APP_INITIALIZATION_STATE` script block, which contains the list name, description, creator, and every place entry. When requested, the tool can also spin up a headless Chromium instance via Playwright to interact with each card and capture additional metadata such as ratings, phone numbers, websites, and plus codes.

## Requirements

* [.NET 10 SDK (Release Candidate or later)](https://dotnet.microsoft.com/)
* Network access to reach `maps.app.goo.gl` and `google.com`
* (Optional, for detailed scraping) [Playwright CLI](https://playwright.dev/dotnet/docs/intro) runtime installed via `playwright install`

## Usage

```bash
Tripperist.MapsListConverter --inputList "{URL of Shared List}" 
```

### Arguments

| Argument              | Required | Description                                                                 |
|-----------------------|----------|-----------------------------------------------------------------------------|
| `--inputList <url>`   | Yes      | Google Maps list URL to download and convert into KML.                      |
| `--outputFile <path>` | No       | Path to the KML file to create. Defaults to the list name with a .kml extension. |
| `--csv`               | No       | Also export the list as a CSV file.                                         |
| `--verbose`           | No       | Enables verbose logging for troubleshooting.                                |
| `--detailed`          | No       | Launches a headless browser to harvest richer per-place metadata (requires Playwright). |
| `--help`, `-h`        | No       | Displays usage information.                                                 |

### Example

```bash
Tripperist.MapsListConverter --inputList "https://maps.app.goo.gl/Dr5BWZN1Z1RL2fu3A" --verbose
```

The command above will create a file named `2023.03.21.MSYDelta.kml` (based on the list name) in the current directory unless `--outputFile` is specified. If `--csv` is provided, a CSV file with the same base name will also be created.

### Detailed scraping mode

Supply the `--detailed` flag to use Microsoft Playwright for a richer scrape:

```bash
playwright install
Tripperist.MapsListConverter --inputList "https://maps.app.goo.gl/Dr5BWZN1Z1RL2fu3A" --detailed
```

The first command installs the Chromium binary that Playwright requires. The converter then launches the browser headlessly, scrolls through every card, and opens each place to extract ratings, review counts, phone numbers, websites, opening hours, and plus codes in addition to the core fields. Expect the detailed mode to take longer than the HTML-only scraper and to consume more CPU and memory.

## Notes

* The default scraper relies on HtmlAgilityPack so it can robustly locate the initialization script, even if Google changes HTML whitespace or formatting.
* The Playwright-powered detailed scraper falls back to the initialization payload when interactive selectors are missing, ensuring the app still produces output.
* The generated KML file stores address and notes inside the placemark description. Latitude and longitude are added whenever Google exposes them in the payload. When `--detailed` is used, the CSV export also includes rating, review count, phone, website, opening hours, and plus code columns.
