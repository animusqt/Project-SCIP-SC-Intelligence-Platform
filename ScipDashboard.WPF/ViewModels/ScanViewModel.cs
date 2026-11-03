using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;
using ScipDashboard.Core.Data;
using ScipDashboard.Core.Models;
using ScipDashboard.WPF.Services;

namespace ScipDashboard.WPF.ViewModels
{
    public class ScanViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        private readonly ScreenScannerService _screenScanner;
        private string _scanInput = string.Empty;
        private string _scanFileName = string.Empty;
        private string _statusMessage = string.Empty;
        private string _scanStationName = string.Empty;
        private string _scanSystemName = "Stanton";
        private string _selectedObservationTab = "Buy tab — price and stock";
        private bool _isProcessing;
        private ObservableCollection<ScanResultItem> _results = new();

        public ScanViewModel(IDataService dataService)
            : this(dataService, new ScreenScannerService())
        {
        }

        public ScanViewModel(IDataService dataService, ScreenScannerService screenScanner)
        {
            _dataService = dataService;
            _screenScanner = screenScanner;
            Results.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsSaveEnabled));
            ScanInputCommand = new RelayCommand(async _ => await ProcessScanInputAsync());
            SaveToMarketCommand = new RelayCommand(async _ => await SaveResultsToMarketAsync());
            ExportResultsCommand = new RelayCommand(_ => ExportResults());
            RemoveResultCommand = new RelayCommand<ScanResultItem?>(RemoveResult);
            ClearCommand = new RelayCommand(_ => ClearScan());
            BrowseFileCommand = new RelayCommand(async _ => await BrowseFileAsync());
            CaptureRegionCommand = new RelayCommand(async _ => await CaptureSavedRegionAsync());
            ObservationTabs = new ObservableCollection<string>
            {
                "Buy tab — price and stock",
                "Local Value — sell price and cargo sizes"
            };
            StarSystems = new ObservableCollection<string> { "Stanton", "Nyx", "Pyro" };
        }

        public string ScanInput
        {
            get => _scanInput;
            set { if (_scanInput != value) { _scanInput = value; OnPropertyChanged(); } }
        }

        public string ScanFileName
        {
            get => _scanFileName;
            set { if (_scanFileName != value) { _scanFileName = value; OnPropertyChanged(); } }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
        }

        public string ScanStationName
        {
            get => _scanStationName;
            set { if (_scanStationName != value) { _scanStationName = value; OnPropertyChanged(); } }
        }

        public string ScanSystemName
        {
            get => _scanSystemName;
            set { if (_scanSystemName != value) { _scanSystemName = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<string> ObservationTabs { get; }
        public ObservableCollection<string> StarSystems { get; }
        public string SelectedObservationTab
        {
            get => _selectedObservationTab;
            set { if (_selectedObservationTab != value) { _selectedObservationTab = value; OnPropertyChanged(); } }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (_isProcessing == value) return;
                _isProcessing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSaveEnabled));
            }
        }

        public ObservableCollection<ScanResultItem> Results
        {
            get => _results;
            set
            {
                if (_results == value) return;
                _results = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSaveEnabled));
            }
        }

        public bool IsSaveEnabled => Results.Count > 0 && !IsProcessing;
        public string ScannerRegionSummary => _screenScanner.GetSavedRegion() is { } region
            ? $"Saved game region: {region.Width} × {region.Height} px at ({region.Left}, {region.Top}) · F9 scans"
            : "No game region saved · press F8 to select the terminal table";

        public void NotifyScannerRegionChanged() => OnPropertyChanged(nameof(ScannerRegionSummary));

        public ICommand ScanInputCommand { get; }
        public ICommand SaveToMarketCommand { get; }
        public ICommand ExportResultsCommand { get; }
        public ICommand RemoveResultCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand BrowseFileCommand { get; }
        public ICommand CaptureRegionCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task ProcessScanInputAsync()
        {
            if (string.IsNullOrWhiteSpace(ScanInput))
            {
                StatusMessage = "Please paste scanner data (JSON, CSV, JS, PY) or upload a file to scan.";
                return;
            }

            IsProcessing = true;
            StatusMessage = "Processing scan data...";
            try
            {
                var marketData = await _dataService.GetMarketDataAsync();
                var stationName = ScanStationName.Trim();
                var systemName = ScanSystemName.Trim();
                var observationTab = SelectedObservationTab;
                var parsed = await Task.Run(() =>
                {
                    var results = ParseScanData(ScanInput);
                    return results.Count > 0
                        ? results
                        : ParseOcrText(ScanInput, marketData, stationName, systemName, observationTab);
                });

                foreach (var item in parsed)
                {
                    if (!string.IsNullOrWhiteSpace(stationName))
                    {
                        item.LocationName = stationName;
                        item.LocationId = ToCommodityId(stationName);
                        item.SystemName = string.IsNullOrWhiteSpace(systemName) ? "Unknown" : systemName;
                    }
                    if (item.SourceName == "Windows OCR")
                        item.ScanType = observationTab;
                }

                Results.Clear();
                foreach (var item in parsed)
                    Results.Add(item);

                OnPropertyChanged(nameof(IsSaveEnabled));
                StatusMessage = parsed.Count == 0
                    ? "No commodity rows were recognized. Correct the text or edit the OCR source, then scan again."
                    : $"Review {parsed.Count} recognized row(s). Values are staged and have not changed market data yet.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Scan error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        public async Task ProcessCapturedImageAsync(string imagePath)
        {
            if (IsProcessing) return;

            try
            {
                IsProcessing = true;
                ScanFileName = "In-game screen capture";
                StatusMessage = "Recognizing the saved in-game region...";
                ScanInput = await RecognizeImageAsync(imagePath);
                await ProcessScanInputAsync();
                if (Results.Count > 0)
                    StatusMessage = $"Staged {Results.Count} in-game row(s). Review and apply them to the market before route planning.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"In-game scan failed: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
                OnPropertyChanged(nameof(ScannerRegionSummary));
            }
        }

        private async Task CaptureSavedRegionAsync()
        {
            string imagePath;
            try
            {
                imagePath = _screenScanner.CaptureSavedRegionToTemporaryPng();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Screen capture failed: {ex.Message}";
                return;
            }

            try
            {
                await ProcessCapturedImageAsync(imagePath);
            }
            finally
            {
                try
                {
                    if (File.Exists(imagePath))
                        File.Delete(imagePath);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    StatusMessage = $"Scan completed, but its temporary screenshot could not be deleted: {ex.Message}";
                }
            }
        }

        private async Task BrowseFileAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Scanner images and data (*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.json;*.csv;*.js;*.py;*.txt)|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.json;*.csv;*.js;*.py;*.txt|Images (*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|All files (*.*)|*.*",
                Title = "Select scanner data file",
                Multiselect = false
            };
            if (dialog.ShowDialog() != true) return;

            var filePath = dialog.FileName;
            ScanFileName = System.IO.Path.GetFileName(filePath);
            IsProcessing = true;
            try
            {
                if (IsImageFile(filePath))
                {
                    StatusMessage = "Recognizing text from image...";
                    ScanInput = await RecognizeImageAsync(filePath);
                    await ProcessScanInputAsync();
                }
                else
                {
                    ScanInput = await System.IO.File.ReadAllTextAsync(filePath);
                    StatusMessage = $"Loaded {ScanFileName}. Extract rows to stage editable prices.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"File/OCR error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private static bool IsImageFile(string path) =>
            new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff" }
                .Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

        private static async Task<string> RecognizeImageAsync(string path)
        {
            var engine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (engine == null)
            {
                var languages = string.Join(", ", OcrEngine.AvailableRecognizerLanguages.Select(language => language.LanguageTag));
                throw new InvalidOperationException(
                    $"Windows OCR has no installed recognizer language. Install an OCR language pack in Windows Settings. Available: {languages}");
            }

            var file = await StorageFile.GetFileFromPathAsync(path);
            using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var result = await engine.RecognizeAsync(bitmap);
            var text = string.Join(Environment.NewLine, result.Lines
                .OrderBy(line => line.Words.FirstOrDefault()?.BoundingRect.Y ?? 0)
                .Select(line => line.Text));
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Windows OCR did not find readable text in this image.");

            return text;
        }

        private void ClearScan()
        {
            ScanInput = string.Empty;
            ScanFileName = string.Empty;
            Results.Clear();
            StatusMessage = string.Empty;
            OnPropertyChanged(nameof(IsSaveEnabled));
        }

        private async Task SaveResultsToMarketAsync()
        {
            if (Results.Count == 0)
            {
                StatusMessage = "Scan or import data before applying it to the market.";
                return;
            }

            if (Results.Any(item =>
                    string.Equals(item.SourceName, "Windows OCR", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.LocationName, "OCR Import", StringComparison.OrdinalIgnoreCase)) &&
                string.IsNullOrWhiteSpace(ScanStationName))
            {
                StatusMessage = "Enter the station scanned in the Station field before applying OCR rows.";
                return;
            }

            var invalid = Results.FirstOrDefault(item =>
                string.IsNullOrWhiteSpace(item.CommodityName) ||
                string.IsNullOrWhiteSpace(item.LocationName) ||
                item.BuyPrice < 0 || item.SellPrice < 0 ||
                item.Volume < 0 || item.DemandVolume < 0);
            if (invalid != null)
            {
                StatusMessage = $"Correct invalid or missing values for '{invalid.CommodityName}' before applying.";
                return;
            }

            IsProcessing = true;
            var saved = 0;
            try
            {
                var marketData = await _dataService.GetMarketDataAsync();
                foreach (var item in Results.ToList())
                {
                    if (string.Equals(item.SourceName, "Windows OCR", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(item.LocationName, "OCR Import", StringComparison.OrdinalIgnoreCase))
                    {
                        item.LocationName = ScanStationName.Trim();
                        item.LocationId = ToCommodityId(item.LocationName);
                        item.SystemName = string.IsNullOrWhiteSpace(ScanSystemName) ? "Unknown" : ScanSystemName.Trim();
                    }
                    item.CommodityId = string.IsNullOrWhiteSpace(item.CommodityId)
                        ? ToCommodityId(item.CommodityName)
                        : item.CommodityId;
                    item.LocationId = string.IsNullOrWhiteSpace(item.LocationId)
                        ? ToCommodityId(item.LocationName)
                        : item.LocationId;
                    var isOcr = string.Equals(item.SourceName, "Windows OCR", StringComparison.OrdinalIgnoreCase);
                    var isBuyTab = string.Equals(item.ScanType, ObservationTabs[0], StringComparison.Ordinal);
                    var existingObservation = marketData.PriceObservations
                        .Where(observation =>
                            string.Equals(observation.CommodityId, item.CommodityId, StringComparison.OrdinalIgnoreCase) &&
                            (string.Equals(observation.LocationId, item.LocationId, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(observation.LocationName, item.LocationName, StringComparison.OrdinalIgnoreCase)))
                        .OrderByDescending(observation => observation.Timestamp)
                        .FirstOrDefault();
                    var buyPrice = isOcr && !isBuyTab ? existingObservation?.BuyPrice ?? 0m : item.BuyPrice;
                    var sellPrice = isOcr && isBuyTab ? existingObservation?.SellPrice ?? 0m : item.SellPrice;
                    var volume = isOcr && !isBuyTab ? existingObservation?.Volume ?? 0 : item.Volume;
                    var demandVolume = isOcr && isBuyTab ? existingObservation?.DemandVolume ?? 0 : item.DemandVolume;
                    await _dataService.UpdateCommodityPriceAsync(
                        item.CommodityId,
                        item.CommodityName,
                        item.LocationId,
                        item.LocationName,
                        string.IsNullOrWhiteSpace(item.SystemName) ? "Unknown" : item.SystemName,
                        buyPrice,
                        sellPrice,
                        volume,
                        demandVolume,
                        DateTime.UtcNow,
                        item.SourceName);
                    item.Status = "Applied";
                    saved++;
                }

                StatusMessage = $"Applied {saved} reviewed price row(s) to the local market.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Market update failed after {saved} row(s): {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void RemoveResult(ScanResultItem? item)
        {
            if (item != null)
                Results.Remove(item);
            OnPropertyChanged(nameof(IsSaveEnabled));
        }

        private void ExportResults()
        {
            if (Results.Count == 0)
            {
                StatusMessage = "There are no reviewed rows to export.";
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv",
                FileName = "scip-ocr-review.csv",
                Title = "Export reviewed scan rows"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var lines = new List<string>
                {
                    "Commodity,Location,System,ScanType,BuyPrice,SellPrice,SupplyScu,DemandScu,ContainerSizes,EstimatedLoadingTime,Confidence"
                };
                lines.AddRange(Results.Select(item => string.Join(",",
                    Csv(item.CommodityName), Csv(item.LocationName), Csv(item.SystemName), Csv(item.ScanType),
                    item.BuyPrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    item.SellPrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    item.Volume, item.DemandVolume, Csv(item.SupportedContainerSizesText),
                    Csv(item.EstimatedLoadingTimeText),
                    item.Confidence.ToString("P0", System.Globalization.CultureInfo.InvariantCulture))));
                System.IO.File.WriteAllLines(dialog.FileName, lines, Encoding.UTF8);
                StatusMessage = $"Exported {Results.Count} reviewed row(s) to {dialog.FileName}.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }

        private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

        private static string ToCommodityId(string value) =>
            Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');

        private List<ScanResultItem> ParseScanData(string input)
        {
            var results = new List<ScanResultItem>();

            try
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var jsonScan = JsonSerializer.Deserialize<ScanData[]>(input, jsonOptions);
                if (jsonScan != null && jsonScan.Length > 0)
                {
                    foreach (var s in jsonScan)
                    {
                        results.Add(MapScanDataToResult(s));
                    }
                    return results;
                }
            }
            catch (JsonException)
            {
                // Continue with the supported text-based import formats.
            }

            var dataHubResult = ParseDataHubFormat(input);
            if (dataHubResult.Count > 0) return dataHubResult;

            var jsResult = ParseJsFormat(input);
            if (jsResult.Count > 0) return jsResult;

            var pyResult = ParsePythonFormat(input);
            if (pyResult.Count > 0) return pyResult;

            var csvResult = ParseCsvFormat(input);
            if (csvResult.Count > 0) return csvResult;

            return results;
        }

        private List<ScanResultItem> ParseOcrText(
            string input,
            MarketData marketData,
            string stationName,
            string systemName,
            string observationTab)
        {
            var results = new List<ScanResultItem>();
            var commodities = marketData.Commodities
                .OrderByDescending(commodity => commodity.Name.Length)
                .ToList();
            var location = string.IsNullOrWhiteSpace(stationName) ? "OCR Import" : stationName;
            var system = string.IsNullOrWhiteSpace(systemName) ? "Unknown" : systemName;
            var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => Regex.Replace(line, @"\s+", " ").Trim())
                .Where(line => line.Length > 0)
                .ToList();
            var anchors = new List<(int Index, Commodity Commodity)>();

            for (var index = 0; index < lines.Count; index++)
            {
                var commodity = commodities.FirstOrDefault(candidate =>
                    lines[index].Contains(candidate.Name, StringComparison.OrdinalIgnoreCase));
                if (commodity != null)
                    anchors.Add((index, commodity));
            }

            var isBuyTab = observationTab.StartsWith("Buy tab", StringComparison.Ordinal);
            for (var rowIndex = 0; rowIndex < anchors.Count; rowIndex++)
            {
                var (start, commodity) = anchors[rowIndex];
                var end = rowIndex + 1 < anchors.Count
                    ? anchors[rowIndex + 1].Index
                    : Math.Min(lines.Count, start + 9);
                var rowText = string.Join(" ", lines.Skip(start).Take(Math.Min(end - start, 9)));

                var priceMatch = Regex.Match(rowText,
                    @"(?<price>\d[\d,]*(?:\.\d+)?)\s*/\s*(?:SCU|UNIT(?:S)?)",
                    RegexOptions.IgnoreCase);
                var price = priceMatch.Success ? ParseOcrNumber(priceMatch.Groups["price"].Value) : 0m;
                if (price <= 0m)
                    continue;

                var quantityMatch = Regex.Match(rowText,
                    @"SHOP\s+QUANTITY\s*[:\-]?\s*(?<quantity>\d[\d,]*)\s*SCU(?!\s*/)",
                    RegexOptions.IgnoreCase);
                var quantity = quantityMatch.Success
                    ? ParseOcrInteger(quantityMatch.Groups["quantity"].Value)
                    : 0;
                var containerSizes = ParseContainerSizes(rowText);
                var loadingTime = ParseLoadingTime(rowText);
                var confidence = 0.35d + 0.25d + 0.2d +
                                 (quantityMatch.Success ? 0.1d : 0d) +
                                 (containerSizes.Count > 0 ? 0.1d : 0d);

                results.Add(new ScanResultItem
                {
                    CommodityId = commodity.Id,
                    CommodityName = commodity.Name,
                    LocationId = ToCommodityId(location),
                    LocationName = location,
                    SystemName = system,
                    BuyPrice = isBuyTab ? price : 0m,
                    SellPrice = isBuyTab ? 0m : price,
                    Volume = isBuyTab ? quantity : 0,
                    DemandVolume = 0,
                    SupportedContainerSizes = containerSizes,
                    EstimatedLoadingTimeSeconds = loadingTime,
                    ScanType = observationTab,
                    Timestamp = DateTime.UtcNow,
                    SourceName = "Windows OCR",
                    Confidence = confidence,
                    Status = "Review"
                });
            }

            return results;
        }

        private static decimal ParseOcrNumber(string value) =>
            decimal.TryParse(value.Replace(",", string.Empty),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed) ? parsed : 0m;

        private static int ParseOcrInteger(string value) =>
            int.TryParse(value.Replace(",", string.Empty),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed) ? parsed : 0;

        private static List<int> ParseContainerSizes(string text)
        {
            var match = Regex.Match(text,
                @"AVAILABLE\s+CARGO\s+SIZES?\s*\(?(?:SCU)?\)?\s*[:\-]?\s*(?<sizes>[\d,\s]+)",
                RegexOptions.IgnoreCase);
            if (!match.Success)
                return new List<int>();

            var allowedSizes = new HashSet<int> { 1, 2, 4, 8, 16, 24, 32 };
            return Regex.Matches(match.Groups["sizes"].Value, @"\d+")
                .Select(value => int.TryParse(value.Value, out var size) ? size : 0)
                .Where(allowedSizes.Contains)
                .Distinct()
                .OrderBy(size => size)
                .ToList();
        }

        private static int? ParseLoadingTime(string text)
        {
            var match = Regex.Match(text,
                @"(?:EST(?:IMATED)?\.?\s+)?LOADING\s+TIME\s*[:\-]?\s*(?<time>\d{1,2}:\d{2}:\d{2})",
                RegexOptions.IgnoreCase);
            if (!match.Success || !TimeSpan.TryParse(match.Groups["time"].Value, out var duration) ||
                duration < TimeSpan.Zero || duration.TotalSeconds > int.MaxValue)
                return null;

            return (int)duration.TotalSeconds;
        }

        private List<ScanResultItem> ParseDataHubFormat(string input)
        {
            var results = new List<ScanResultItem>();

            try
            {
                var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                bool inCommoditiesSection = false;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();

                    if (trimmed.Equals("COMMODITIES", StringComparison.OrdinalIgnoreCase))
                    {
                        inCommoditiesSection = true;
                        continue;
                    }

                    if (!inCommoditiesSection) continue;

                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                    if (trimmed.StartsWith("==") || trimmed.StartsWith("---")) continue;

                    var cols = trimmed.Split('|');
                    if (cols.Length < 3) continue;

                    var commodityName = cols[0].Trim('"', ' ');
                    if (string.IsNullOrWhiteSpace(commodityName)) continue;
                    if (commodityName.Equals("Commodity", StringComparison.OrdinalIgnoreCase)) continue;

                    var buyPriceStr = cols.Length > 2 ? cols[2].Trim('"', ' ') : string.Empty;
                    var sellPriceStr = cols.Length > 3 ? cols[3].Trim('"', ' ') : string.Empty;

                    decimal buyPrice = ParseAuecPrice(buyPriceStr);
                    decimal sellPrice = ParseAuecPrice(sellPriceStr);

                    if (buyPrice <= 0 && sellPrice <= 0) continue;

                    results.Add(new ScanResultItem
                    {
                        CommodityId = commodityName.ToLowerInvariant().Replace(" ", "-"),
                        CommodityName = commodityName,
                        LocationId = "datahub",
                        LocationName = "SC DataHub",
                        SystemName = "Stanton",
                        BuyPrice = buyPrice,
                        SellPrice = sellPrice,
                        Volume = 0,
                        DemandVolume = 0,
                        Timestamp = DateTime.UtcNow,
                        SourceName = "SC DataHub",
                        Confidence = 0.95,
                        Status = "Review"
                    });
                }

                return results;
            }
            catch { return results; }
        }

        private decimal ParseAuecPrice(string priceStr)
        {
            if (string.IsNullOrWhiteSpace(priceStr)) return 0m;
            if (priceStr.Trim() == "—") return 0m;

            var cleaned = priceStr.Replace("aUEC", "").Replace("aUEC", "").Replace(",", "").Trim();
            if (decimal.TryParse(cleaned, out decimal result))
                return result;

            return 0m;
        }

        private List<ScanResultItem> ParseJsFormat(string input)
        {
            var results = new List<ScanResultItem>();

            try
            {
                var arrayMatch = Regex.Match(input, @"(?:const|let|var|module\.exports|export\s+default)\s+(\w+)\s*=\s*(\[[\s\S]*?\])\s*;?", RegexOptions.Multiline);
                if (!arrayMatch.Success)
                {
                    arrayMatch = Regex.Match(input, @"(\[[\s\S]*?\])");
                    if (!arrayMatch.Success) return results;
                }

                var arrayContent = arrayMatch.Groups[2].Success ? arrayMatch.Groups[2].Value : arrayMatch.Groups[1].Value;
                arrayContent = CleanJsArrayContent(arrayContent);

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var jsonScan = JsonSerializer.Deserialize<ScanData[]>(arrayContent, jsonOptions);
                if (jsonScan != null && jsonScan.Length > 0)
                {
                    foreach (var s in jsonScan)
                    {
                        results.Add(MapScanDataToResult(s));
                    }
                    return results;
                }
            }
            catch { }

            return results;
        }

        private List<ScanResultItem> ParsePythonFormat(string input)
        {
            var results = new List<ScanResultItem>();

            try
            {
                var listMatch = Regex.Match(input, @"(?:COMMODITY_DATA|data|prices|commodities)\s*=\s*(\[[\s\S]*?\])", RegexOptions.Multiline);
                if (!listMatch.Success)
                {
                    listMatch = Regex.Match(input, @"(\[[\s\S]*?\])");
                    if (!listMatch.Success) return results;
                }

                var listContent = listMatch.Groups[1].Value;
                listContent = CleanPythonListContent(listContent);

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var jsonScan = JsonSerializer.Deserialize<ScanData[]>(listContent, jsonOptions);
                if (jsonScan != null && jsonScan.Length > 0)
                {
                    foreach (var s in jsonScan)
                    {
                        results.Add(MapScanDataToResult(s));
                    }
                    return results;
                }
            }
            catch { }

            return results;
        }

        private List<ScanResultItem> ParseCsvFormat(string input)
        {
            var results = new List<ScanResultItem>();

            var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return results;

            var headers = lines[0].Split(',').Select(h => h.Trim('"', ' ', '\t').ToLowerInvariant()).ToList();
            int startIndex = headers.Contains("commodityid") || headers.Contains("commodity") ? 1 : 0;

            for (int i = startIndex; i < lines.Length; i++)
            {
                var cols = lines[i].Split(',');
                if (cols.Length < 8) continue;

                try
                {
                    var commodityId = cols[0].Trim('"', ' ');
                    var commodityName = cols[1].Trim('"', ' ');
                    var locationId = cols[2].Trim('"', ' ');
                    var locationName = cols[3].Trim('"', ' ');
                    var systemName = cols.Length > 4 ? cols[4].Trim('"', ' ') : locationName;
                    var buyPrice = decimal.Parse(cols.Length > 5 ? cols[5].Trim('"', ' ') : "0");
                    var sellPrice = decimal.Parse(cols.Length > 6 ? cols[6].Trim('"', ' ') : "0");
                    var volume = int.TryParse(cols.Length > 7 ? cols[7].Trim('"', ' ') : "0", out int v) ? v : 0;
                    var demandVolume = int.TryParse(cols.Length > 8 ? cols[8].Trim('"', ' ') : "0", out int d) ? d : 0;

                    results.Add(new ScanResultItem
                    {
                        CommodityId = commodityId,
                        CommodityName = commodityName,
                        LocationId = locationId,
                        LocationName = locationName,
                        SystemName = systemName,
                        BuyPrice = buyPrice,
                        SellPrice = sellPrice,
                        Volume = volume,
                        DemandVolume = demandVolume,
                        Timestamp = DateTime.UtcNow,
                        SourceName = "SCANNER",
                        Confidence = 0.95,
                        Status = "Review"
                    });
                }
                catch { }
            }

            return results;
        }

        private string CleanJsArrayContent(string content)
        {
            content = Regex.Replace(content, @"//.*?$", "", RegexOptions.Multiline);
            content = Regex.Replace(content, @"/\*.*?\*/", "", RegexOptions.Singleline);
            content = Regex.Replace(content, @"^\s*(?:const|let|var|module\.exports|export\s+default)\s+\w+\s*=\s*", "", RegexOptions.Multiline);
            content = content.TrimEnd(';', ' ', '\t', '\r', '\n');
            return content.Trim();
        }

        private string CleanPythonListContent(string content)
        {
            content = Regex.Replace(content, @"#.*?$", "", RegexOptions.Multiline);
            content = content.TrimEnd(';', ' ', '\t', '\r', '\n');
            return content.Trim();
        }

        private ScanResultItem MapScanDataToResult(ScanData s)
        {
            return new ScanResultItem
            {
                CommodityId = s.CommodityId ?? "unknown",
                CommodityName = s.CommodityName ?? s.CommodityId ?? "unknown",
                LocationId = s.LocationId ?? "unknown",
                LocationName = s.LocationName ?? s.LocationId ?? "unknown",
                SystemName = s.SystemName ?? "Unknown",
                BuyPrice = s.BuyPrice,
                SellPrice = s.SellPrice,
                Volume = s.Volume,
                DemandVolume = s.DemandVolume,
                SupportedContainerSizes = s.SupportedContainerSizes,
                EstimatedLoadingTimeSeconds = s.EstimatedLoadingTimeSeconds,
                Timestamp = s.Timestamp == default ? DateTime.UtcNow : s.Timestamp,
                SourceName = "SCANNER",
                Confidence = 1,
                Status = "Review"
            };
        }

        private void DispatcherInvoke(Action action)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(action);
        }
    }

    public class ScanData
    {
        public string? CommodityId { get; set; }
        public string? CommodityName { get; set; }
        public string? LocationId { get; set; }
        public string? LocationName { get; set; }
        public string? SystemName { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public int Volume { get; set; }
        public int DemandVolume { get; set; }
        public List<int> SupportedContainerSizes { get; set; } = new();
        public int? EstimatedLoadingTimeSeconds { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ScanResultItem : INotifyPropertyChanged
    {
        private string _commodityId = string.Empty;
        private string _commodityName = string.Empty;
        private string _locationId = string.Empty;
        private string _locationName = string.Empty;
        private string _systemName = string.Empty;
        private decimal _buyPrice;
        private decimal _sellPrice;
        private int _volume;
        private int _demandVolume;
        private List<int> _supportedContainerSizes = new();
        private int? _estimatedLoadingTimeSeconds;
        private double _confidence;
        private string _status = string.Empty;

        public string CommodityId { get => _commodityId; set => Set(ref _commodityId, value); }
        public string CommodityName { get => _commodityName; set => Set(ref _commodityName, value); }
        public string LocationId { get => _locationId; set => Set(ref _locationId, value); }
        public string LocationName { get => _locationName; set => Set(ref _locationName, value); }
        public string SystemName { get => _systemName; set => Set(ref _systemName, value); }
        public decimal BuyPrice
        {
            get => _buyPrice;
            set { if (Set(ref _buyPrice, value)) { OnPropertyChanged(nameof(Margin)); OnPropertyChanged(nameof(MarginText)); } }
        }
        public decimal SellPrice
        {
            get => _sellPrice;
            set { if (Set(ref _sellPrice, value)) { OnPropertyChanged(nameof(Margin)); OnPropertyChanged(nameof(MarginText)); } }
        }
        public int Volume { get => _volume; set => Set(ref _volume, value); }
        public int DemandVolume { get => _demandVolume; set => Set(ref _demandVolume, value); }
        public List<int> SupportedContainerSizes
        {
            get => _supportedContainerSizes;
            set => Set(ref _supportedContainerSizes, value);
        }
        public string SupportedContainerSizesText
        {
            get => string.Join(", ", SupportedContainerSizes);
            set
            {
                var allowedSizes = new HashSet<int> { 1, 2, 4, 8, 16, 24, 32 };
                SupportedContainerSizes = Regex.Matches(value ?? string.Empty, @"\d+")
                    .Select(match => int.TryParse(match.Value, out var size) ? size : 0)
                    .Where(allowedSizes.Contains)
                    .Distinct()
                    .OrderBy(size => size)
                    .ToList();
            }
        }
        public int? EstimatedLoadingTimeSeconds
        {
            get => _estimatedLoadingTimeSeconds;
            set
            {
                if (Set(ref _estimatedLoadingTimeSeconds, value))
                    OnPropertyChanged(nameof(EstimatedLoadingTimeText));
            }
        }
        public string EstimatedLoadingTimeText
        {
            get => EstimatedLoadingTimeSeconds is int seconds
                ? TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss")
                : string.Empty;
            set => EstimatedLoadingTimeSeconds = TimeSpan.TryParse(value, out var duration) &&
                duration >= TimeSpan.Zero && duration.TotalSeconds <= int.MaxValue
                    ? (int)duration.TotalSeconds
                    : null;
        }
        public decimal Margin => BuyPrice > 0 ? Math.Round(((SellPrice - BuyPrice) / BuyPrice) * 100, 2) : 0m;
        public DateTime Timestamp { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string ScanType { get; set; } = string.Empty;
        public string Status { get => _status; set => Set(ref _status, value); }
        public double Confidence { get => _confidence; set { if (Set(ref _confidence, value)) OnPropertyChanged(nameof(ConfidenceText)); } }
        public string MarginText => $"{Margin:+0.##;-0.##;0}%";
        public string BuyText => $"{BuyPrice:0}";
        public string SellText => $"{SellPrice:0}";
        public string ConfidenceText => $"{Confidence:P0}";

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ComingSoonItem
    {
        public string FeatureName { get; set; } = string.Empty;
        public string FeatureDescription { get; set; } = string.Empty;
    }
}
