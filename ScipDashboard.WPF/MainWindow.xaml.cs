// SCIP Dashboard - MainWindow Code Behind
// Native C# WPF (no Electron/Python)

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using ScipDashboard.Core;
using ScipDashboard.WPF;
using ScipDashboard.WPF.Modules;
using ScipDashboard.WPF.Services;
using ScipDashboard.WPF.ViewModels;
using DashboardView = ScipDashboard.WPF.DashboardView;

namespace ScipDashboard.Main
{
    public partial class MainWindow : Window
    {
        private const int WmHotkey = 0x0312;
        private const int CalibrateHotkeyId = 0x5341;
        private const int ScanHotkeyId = 0x5342;
        private const uint ModifierNoRepeat = 0x4000;
        private const uint VirtualKeyF8 = 0x77;
        private const uint VirtualKeyF9 = 0x78;

        private bool _isLoading = true;
        private HwndSource? _windowSource;
        private bool _calibrateHotkeyRegistered;
        private bool _scanHotkeyRegistered;
        
        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += MainWindow_SourceInitialized;
            Closed += MainWindow_Closed;
            // Start loading animation when window is loaded
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            _windowSource = HwndSource.FromHwnd(handle);
            _windowSource?.AddHook(WindowMessageHook);

            _calibrateHotkeyRegistered = RegisterHotKey(handle, CalibrateHotkeyId, ModifierNoRepeat, VirtualKeyF8);
            _scanHotkeyRegistered = RegisterHotKey(handle, ScanHotkeyId, ModifierNoRepeat, VirtualKeyF9);
            if (!_calibrateHotkeyRegistered || !_scanHotkeyRegistered)
            {
                var unavailable = !_calibrateHotkeyRegistered && !_scanHotkeyRegistered
                    ? "F8 and F9 are already registered by another application."
                    : !_calibrateHotkeyRegistered
                        ? "F8 is already registered by another application."
                        : "F9 is already registered by another application.";
                App.Services.Get<ScanViewModel>().StatusMessage =
                    $"Game scanner hotkeys unavailable: {unavailable} Use the scanner page buttons instead.";
            }
        }

        private IntPtr WindowMessageHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message != WmHotkey) return IntPtr.Zero;

            var hotkeyId = wParam.ToInt32();
            if (hotkeyId == CalibrateHotkeyId && _calibrateHotkeyRegistered)
            {
                handled = true;
                Dispatcher.BeginInvoke(new Action(CalibrateGameScreenRegion));
            }
            else if (hotkeyId == ScanHotkeyId && _scanHotkeyRegistered)
            {
                handled = true;
                Dispatcher.BeginInvoke(new Action(async () => await ScanSavedGameRegionAsync()));
            }

            return IntPtr.Zero;
        }

        private void CalibrateGameScreenRegion()
        {
            try
            {
                var overlay = new RegionCalibrationWindow { Owner = this };
                if (overlay.ShowDialog() != true || overlay.SelectedRegion == null)
                    return;

                App.Services.Get<ScreenScannerService>().SaveRegion(overlay.SelectedRegion);
                var viewModel = App.Services.Get<ScanViewModel>();
                viewModel.NotifyScannerRegionChanged();
                viewModel.StatusMessage = "Game capture region saved. Press F9 over the in-game commodity terminal to scan.";
            }
            catch (Exception ex)
            {
                App.Services.Get<ScanViewModel>().StatusMessage = $"Could not save the game capture region: {ex.Message}";
            }
        }

        private async Task ScanSavedGameRegionAsync()
        {
            string imagePath;
            var viewModel = App.Services.Get<ScanViewModel>();
            try
            {
                imagePath = App.Services.Get<ScreenScannerService>().CaptureSavedRegionToTemporaryPng();
            }
            catch (Exception ex)
            {
                viewModel.StatusMessage = $"Game screen capture failed: {ex.Message}";
                return;
            }

            try
            {
                await viewModel.ProcessCapturedImageAsync(imagePath);
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
                    viewModel.StatusMessage = $"Scan completed, but the temporary screenshot could not be deleted: {ex.Message}";
                }
            }
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            if (_calibrateHotkeyRegistered)
                UnregisterHotKey(handle, CalibrateHotkeyId);
            if (_scanHotkeyRegistered)
                UnregisterHotKey(handle, ScanHotkeyId);
            _windowSource?.RemoveHook(WindowMessageHook);
            _windowSource = null;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(IntPtr window, int id);

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Debug: Log when window loaded event fires
            System.Diagnostics.Debug.WriteLine("[MainWindow] Loaded event fired");
            
            // Start the loading animation when window is loaded
            Task.Run(() => StartLoadingAnimation());
        }

        private async void StartLoadingAnimation()
        {
            try
            {
                _isLoading = true;

                Dispatcher.Invoke(() =>
                {
                    if (LoadingOverlay != null)
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;
                        LoadingBarFill.Width = 0;
                        LoadingProgressText.Text = "0%";
                    }
                });

                // Simulate loading for 2 seconds (100 steps at 20ms each)
                int totalSteps = 100;
                for (int i = 0; i <= totalSteps; i++)
                {
                    await Task.Delay(20);

                    if (!_isLoading) break;

                    double progress = (double)i / totalSteps;
                    double barWidth = progress * 400;
                    int percent = (int)(progress * 100);

                    Dispatcher.Invoke(() =>
                    {
                        if (LoadingBarFill != null)
                        {
                            LoadingBarFill.Width = barWidth;
                        }
                        if (LoadingProgressText != null)
                        {
                            LoadingProgressText.Text = $"{percent}%";
                        }

                        // Update status text at milestones
                        if (i == 25)
                        {
                            var statusText = LoadingOverlay.FindName("StatusText") as TextBlock;
                            if (statusText != null)
                                statusText.Text = "Loading modules...";
                        }
                        else if (i == 50)
                        {
                            var statusText = LoadingOverlay.FindName("StatusText") as TextBlock;
                            if (statusText != null)
                                statusText.Text = "Connecting to data sources...";
                        }
                        else if (i == 75)
                        {
                            var statusText = LoadingOverlay.FindName("StatusText") as TextBlock;
                            if (statusText != null)
                                statusText.Text = "Finalizing...";
                        }
                    });
                }

                _isLoading = false;

                Dispatcher.Invoke(() =>
                {
                    if (LoadingBarFill != null)
                    {
                        LoadingBarFill.Width = 400;
                    }
                    if (LoadingProgressText != null)
                    {
                        LoadingProgressText.Text = "100%";
                    }
                });

                await Task.Delay(300);

                Dispatcher.Invoke(() =>
                {
                    if (LoadingOverlay != null)
                    {
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                    }

                    var viewModel = DataContext as MainViewModel;
                    if (viewModel != null)
                    {
                        viewModel.CurrentView = ScipDashboard.WPF.DashboardView.Main;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Loading animation error: {ex.Message}");
            }
        }

        // Navigation handlers
        private void Navigate_Dashboard(object sender, RoutedEventArgs e) 
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                viewModel.CurrentView = DashboardView.Main;
                DashboardState.Instance.CurrentView = DashboardView.Main;
            }
        }
        
        private void Navigate_Market(object sender, RoutedEventArgs e)
        {
            try
            {
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null)
                {
                    viewModel.CurrentView = DashboardView.Market;
                    DashboardState.Instance.CurrentView = DashboardView.Market;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Navigate_Market] ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Navigate_Market] STACK: {ex.StackTrace}");
                MessageBox.Show($"Market view error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Navigate_Hauling(object sender, RoutedEventArgs e) 
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                viewModel.CurrentView = DashboardView.Hauling;
                DashboardState.Instance.CurrentView = DashboardView.Hauling;
            }
        }

        private void Navigate_RouteBuilder(object sender, RoutedEventArgs e) 
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                viewModel.CurrentView = DashboardView.RouteBuilder;
                DashboardState.Instance.CurrentView = DashboardView.RouteBuilder;
            }
        }

        private void Navigate_MultiRouteBuilder(object sender, RoutedEventArgs e) 
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                viewModel.CurrentView = DashboardView.MultiRouteBuilder;
                DashboardState.Instance.CurrentView = DashboardView.MultiRouteBuilder;
            }
        }

        private void Navigate_SingleRun(object sender, RoutedEventArgs e) 
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                viewModel.CurrentView = DashboardView.SingleRun;
                DashboardState.Instance.CurrentView = DashboardView.SingleRun;
            }
        }
        
        private void Navigate_Fleet(object sender, RoutedEventArgs e) 
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                viewModel.CurrentView = DashboardView.Fleet;
                DashboardState.Instance.CurrentView = DashboardView.Fleet;
            }
        }
        
        private void Navigate_StarQuester(object sender, RoutedEventArgs e) 
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                viewModel.CurrentView = DashboardView.StarQuest;
                DashboardState.Instance.CurrentView = DashboardView.StarQuest;
            }
        }
        
        private void Navigate_Settings(object sender, RoutedEventArgs e) 
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                viewModel.CurrentView = DashboardView.Settings;
                DashboardState.Instance.CurrentView = DashboardView.Settings;
            }
        }

        private void Navigate_OcrScan(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                viewModel.CurrentView = ScipDashboard.WPF.DashboardView.Scan;
                DashboardState.Instance.CurrentView = ScipDashboard.WPF.DashboardView.Scan;
            }
        }

        private void Navigate_Salvaging(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                viewModel.CurrentView = ScipDashboard.WPF.DashboardView.Salvage;
                DashboardState.Instance.CurrentView = ScipDashboard.WPF.DashboardView.Salvage;
            }
        }

        public void NavigateToView(ScipDashboard.WPF.DashboardView targetView)
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                viewModel.CurrentView = targetView;
                DashboardState.Instance.CurrentView = targetView;
            }
        }
    }
}
