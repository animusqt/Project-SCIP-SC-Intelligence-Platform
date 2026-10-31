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
using ScipDashboard.Core.Data;
using ScipDashboard.Core.Models;

namespace ScipDashboard.WPF.ViewModels
{
    public class ScanViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        private string _scanInput = string.Empty;
        private string _scanFileName = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isProcessing;
        private ObservableCollection<ScanResultItem> _results = new();

        public ScanViewModel(IDataService dataService)
        {
            _dataService = dataService;
            ScanInputCommand = new RelayCommand(async _ => await ProcessScanInputAsync());
            ClearCommand = new RelayCommand(_ => ClearScan());
            BrowseFileCommand = new RelayCommand(async _ => await BrowseFileAsync());
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

        public bool IsProcessing
        {
            get => _isProcessing;
            set { if (_isProcessing != value) { _isProcessing = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<ScanResultItem> Results
        {
            get => _results;
            set { if (_results != value) { _results = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<ComingSoonItem> ComingSoonItems { get; } = new()
        {
            new()
            {
                FeatureName = "In-Game Screen Capture",
                FeatureDescription = "Real-time screenshot capture of commodity terminals using OCR to extract prices automatically without manual copy-paste."
            },
            new()
            {
                FeatureName = "Live Price Overlay",
                FeatureDescription = "Floating in-game overlay showing current buy/sell prices and best margins while you play Star Citizen."
            },
            new()
            {
                FeatureName = "Auto Market Sync",
                FeatureDescription = "Automatically push scanned prices to the SCIP Market Overview for community-wide price tracking."
            },
            new()
            {
                FeatureName = "Profit Route Calculator",
                FeatureDescription = "Calculate optimal buy/sell routes across multiple stations with SCU capacity and risk assessment."
            },
            new()
            {
                FeatureName = "Price History Charts",
                FeatureDescription = "Track price trends over time for each commodity at each location with visual charts."
            },
            new()
            {
                FeatureName = "Custom Alerts",
                FeatureDescription = "Set price threshold alerts and get notified when commodities hit your target buy/sell prices."
            },
            new()
            {
                FeatureName = "Multi-Ship Planning",
                FeatureDescription = "Plan hauling runs across multiple ships with combined SCU capacity and coordinated routes."
            },
            new()
            {
                FeatureName = "Export & Share",
                FeatureDescription = "Export scan data to CSV/JSON or share route plans with your org mates."
            }
        };

        public ICommand ScanInputCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand BrowseFileCommand { get; }

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
            Results.Clear();

            try
            {
                await Task.Run(() =>
                {
                    var parsed = ParseScanData(ScanInput);
                    if (parsed.Count == 0)
                    {
                        DispatcherInvoke(() => StatusMessage = "No valid commodity data found in scan input.");
                        return;
                    }

                    foreach (var item in parsed)
                    {
                        _dataService.UpdateCommodityPriceAsync(
                            item.CommodityId,
                            item.LocationId,
                            item.LocationName,
                            item.SystemName,
                            item.BuyPrice,
                            item.SellPrice,
                            item.Volume,
                            item.DemandVolume,
                            item.Timestamp,
                            "SCANNER_IMPORT"
                        );
                    }

                    DispatcherInvoke(() =>
                    {
                        foreach (var item in parsed)
                            Results.Add(item);

                        StatusMessage = $"Scan complete. {parsed.Count} commodity price(s) updated across {parsed.Select(x => x.LocationId).Distinct().Count()} location(s).";
                    });
                });
            }
            catch (Exception ex)
            {
                DispatcherInvoke(() => StatusMessage = $"Scan error: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task BrowseFileAsync()
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Scanner data files (*.json;*.csv;*.js;*.py;*.txt)|*.json;*.csv;*.js;*.py;*.txt|All files (*.*)|*.*",
                    Title = "Select scanner data file",
                    Multiselect = false
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        var filePath = dialog.FileName;
                        ScanFileName = System.IO.Path.GetFileName(filePath);
                        var fileContent = System.IO.File.ReadAllText(filePath);
                        ScanInput = fileContent;
                        StatusMessage = $"Loaded {filePath}";
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"File load error: {ex.Message}";
                    }
                }
            });
        }

        private void ClearScan()
        {
            ScanInput = string.Empty;
            ScanFileName = string.Empty;
            Results.Clear();
            StatusMessage = string.Empty;
        }

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
            catch { }

            var jsResult = ParseJsFormat(input);
            if (jsResult.Count > 0) return jsResult;

            var pyResult = ParsePythonFormat(input);
            if (pyResult.Count > 0) return pyResult;

            var csvResult = ParseCsvFormat(input);
            if (csvResult.Count > 0) return csvResult;

            return results;
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
                        Margin = buyPrice > 0 ? Math.Round(((sellPrice - buyPrice) / buyPrice) * 100, 2) : 0m,
                        Timestamp = DateTime.UtcNow,
                        SourceName = "SCANNER",
                        Status = "Updated"
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
                Margin = s.BuyPrice > 0 ? Math.Round(((s.SellPrice - s.BuyPrice) / s.BuyPrice) * 100, 2) : 0m,
                Timestamp = s.Timestamp == default ? DateTime.UtcNow : s.Timestamp,
                SourceName = "SCANNER",
                Status = "Updated"
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
        public DateTime Timestamp { get; set; }
    }

    public class ScanResultItem : INotifyPropertyChanged
    {
        public string CommodityId { get; set; } = string.Empty;
        public string CommodityName { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public int Volume { get; set; }
        public int DemandVolume { get; set; }
        public decimal Margin { get; set; }
        public DateTime Timestamp { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public string MarginText => $"+{Margin:0.##}%";
        public string BuyText => $"{BuyPrice:0}";
        public string SellText => $"{SellPrice:0}";

        public event PropertyChangedEventHandler? PropertyChanged;
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
