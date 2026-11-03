using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScipDashboard.WPF.Services;

namespace ScipDashboard.WPF.Modules
{
    public partial class RegionCalibrationWindow : Window
    {
        private Point _dragStart;
        private bool _isDragging;

        public ScreenScanRegion? SelectedRegion { get; private set; }

        public RegionCalibrationWindow()
        {
            InitializeComponent();
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(SelectionCanvas);
            _isDragging = true;
            SelectionRectangle.Visibility = Visibility.Visible;
            UpdateSelection(e.GetPosition(SelectionCanvas));
            SelectionCanvas.CaptureMouse();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
                UpdateSelection(e.GetPosition(SelectionCanvas));
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;

            _isDragging = false;
            SelectionCanvas.ReleaseMouseCapture();
            var end = e.GetPosition(SelectionCanvas);
            var startOnScreen = SelectionCanvas.PointToScreen(_dragStart);
            var endOnScreen = SelectionCanvas.PointToScreen(end);
            var left = (int)Math.Round(Math.Min(startOnScreen.X, endOnScreen.X));
            var top = (int)Math.Round(Math.Min(startOnScreen.Y, endOnScreen.Y));
            var width = (int)Math.Round(Math.Abs(endOnScreen.X - startOnScreen.X));
            var height = (int)Math.Round(Math.Abs(endOnScreen.Y - startOnScreen.Y));

            if (width < 40 || height < 30)
            {
                SelectionRectangle.Visibility = Visibility.Collapsed;
                return;
            }

            SelectedRegion = new ScreenScanRegion
            {
                Left = left,
                Top = top,
                Width = width,
                Height = height
            };
            DialogResult = true;
        }

        private void UpdateSelection(Point current)
        {
            var left = Math.Min(_dragStart.X, current.X);
            var top = Math.Min(_dragStart.Y, current.Y);
            Canvas.SetLeft(SelectionRectangle, left);
            Canvas.SetTop(SelectionRectangle, top);
            SelectionRectangle.Width = Math.Abs(current.X - _dragStart.X);
            SelectionRectangle.Height = Math.Abs(current.Y - _dragStart.Y);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            DialogResult = false;
        }
    }
}
