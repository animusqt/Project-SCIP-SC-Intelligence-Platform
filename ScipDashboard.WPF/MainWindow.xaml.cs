// SCIP Dashboard - MainWindow Code Behind
// Native C# WPF (no Electron/Python)

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScipDashboard.Core;
using ScipDashboard.WPF;

namespace ScipDashboard.Main
{
    public partial class MainWindow : Window
    {
        private bool _isLoading = true;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Make window draggable from anywhere
            MouseLeftButtonDown += Window_MouseLeftButtonDown;
            
            // Start loading animation when window is loaded
            Loaded += MainWindow_Loaded;
        }

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
                        viewModel.CurrentView = DashboardView.Main;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Loading animation error: {ex.Message}");
            }
        }

        // Window control handlers - close button minimizes to tray instead of closing
        private void OnMinimizeClick(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        
        private void OnMaximizeClick(object sender, RoutedEventArgs e) 
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        
        private void OnCloseClick(object sender, RoutedEventArgs e) 
        {
            // Close button minimizes to system tray instead of closing
            WindowState = WindowState.Minimized;
        }

        // Window drag handling
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
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
