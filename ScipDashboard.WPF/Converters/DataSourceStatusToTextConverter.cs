using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ScipDashboard.Core.Models;

namespace ScipDashboard.WPF.Converters
{
    public class DataSourceStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataSourceStatus status)
            {
                return status switch
                {
                    DataSourceStatus.Live => "Live",
                    DataSourceStatus.Cached => "Cached",
                    DataSourceStatus.Sample => "Sample",
                    DataSourceStatus.Unavailable => "Unavailable",
                    _ => "Unknown"
                };
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DataSourceStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataSourceStatus status)
            {
                return status switch
                {
                    DataSourceStatus.Live => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27ae60")),
                    DataSourceStatus.Cached => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f39c12")),
                    DataSourceStatus.Sample => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95a5a6")),
                    DataSourceStatus.Unavailable => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e74c3c")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"))
                };
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MarginToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal margin)
            {
                if (margin > 20) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27ae60"));
                if (margin > 10) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ecc71"));
                if (margin > 5) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498db"));
                if (margin > 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f39c12"));
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95a5a6"));
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95a5a6"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DecimalToCurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d)
                return d.ToString("0");
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DateTimeToFriendlyStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                var now = DateTime.UtcNow;
                var diff = now - date;

                if (diff.TotalSeconds < 60)
                    return "Just now";
                if (diff.TotalMinutes < 60)
                    return $"{Math.Floor(diff.TotalMinutes)}m ago";
                if (diff.TotalHours < 24)
                    return $"{Math.Floor(diff.TotalHours)}h ago";
                if (diff.TotalDays < 7)
                    return $"{Math.Floor(diff.TotalDays)}d ago";
                return date.ToString("MMM dd");
            }
            return "Never";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            return System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
