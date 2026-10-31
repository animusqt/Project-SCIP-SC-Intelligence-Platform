using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
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
        private string _statusMessage = string.Empty;
        private bool _isProcessing;
        private ObservableCollection<ScanResultItem> _results = new();

        public ScanViewModel(IDataService dataService)
        {
            _dataService = dataService;
            ScanInputCommand = new RelayCommand(async _ => await ProcessScanInputAsync());
            ClearCommand = new RelayCommand(_ => ClearScan());
        }

        public string ScanInput
        {
            get => _scanInput;
            set { if (_scanInput != value) { _scanInput = value; OnPropertyChanged(); } }
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

        public ICommand ScanInputCommand { get; }
        public ICommand ClearCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task ProcessScanInputAsync()
        {
            if (string.IsNullOrWhiteSpace(ScanInput))
            {
                StatusMessage = "Please paste scanner data (JSON or CSV) to scan.";
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

                    // Apply scan data to market data service
                    foreach (var item in parsed)
                    {
                        // Update existing commodity price observations
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

        private void ClearScan()
        {
            ScanInput = string.Empty;
            Results.Clear();
            StatusMessage = string.Empty;
        }

        private List<ScanResultItem> ParseScanData(string input)
        {
            var results = new List<ScanResultItem>();

            // Try JSON first
            try
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var jsonScan = JsonSerializer.Deserialize<ScanData[]>(input, jsonOptions);
                if (jsonScan != null && jsonScan.Length > 0)
                {
                    foreach (var s in jsonScan)
                    {
                        results.Add(new ScanResultItem
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
                        });
                    }
                    return results;
                }
            }
            catch { /* fall through to CSV */ }

            // Try CSV format: CommodityId,CommodityName,LocationId,LocationName,SystemName,BuyPrice,SellPrice,Volume,DemandVolume
            var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 1)
            {
                var headers = lines[0].Split(',');
                bool hasHeader = headers.Length > 1 && headers[0].ToLower().Contains("commodity");
                int startIndex = hasHeader ? 1 : 0;

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
                        var systemName = cols[3].Trim('"', ' ');
                        var buyPrice = decimal.Parse(cols[4].Trim('"', ' '));
                        var sellPrice = decimal.Parse(cols[5].Trim('"', ' '));
                        var volume = int.Parse(cols[6].Trim('"', ' '));
                        var demandVolume = int.Parse(cols[7].Trim('"', ' '));

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
                    catch { /* skip malformed rows */ }
                }
            }

            return results;
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
}
