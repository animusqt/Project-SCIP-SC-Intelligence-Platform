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
                
                // Debug: Log starting loading animation
                System.Diagnostics.Debug.WriteLine("[MainWindow] Starting loading animation (thread: " + System.Threading.Thread.CurrentThread.Name + ")");
                
                // Show loading screen using Dispatcher to ensure UI thread execution
                Dispatcher.Invoke(() =>
                {
                    if (LoadingOverlay != null)
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;
                        System.Diagnostics.Debug.WriteLine("[MainWindow] LoadingOverlay displayed");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[MainWindow] WARNING: LoadingOverlay is null!");
                    }
                });
                
                // Simulate loading for 1 second (100 steps at 10ms each)
                for (int i = 0; i < 200; i++)
                {
                    await Task.Delay(10);
                    
                    if (!_isLoading) break;
                }
                
                _isLoading = false;
                
                // Debug: Log after loading loop completes
                System.Diagnostics.Debug.WriteLine("[MainWindow] Loading animation complete");
                
                // Hide loading screen and show main content using Dispatcher to ensure UI thread execution
                Dispatcher.Invoke(() =>
                {
                    if (LoadingOverlay != null)
                    {
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                        System.Diagnostics.Debug.WriteLine("[MainWindow] LoadingOverlay hidden");
                    }
                    
                    var viewModel = DataContext as MainViewModel;
                    if (viewModel != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] DataContext bound, ViewModel type: {viewModel.GetType().Name}");
                        try
                        {
                            viewModel.CurrentView = DashboardView.Main;
                            System.Diagnostics.Debug.WriteLine("[MainWindow] CurrentView set to Main");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainWindow] Error setting CurrentView: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[MainWindow] WARNING: DataContext is null!");
                    }
                });
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Loading animation error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace:\n{ex.StackTrace}");
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
