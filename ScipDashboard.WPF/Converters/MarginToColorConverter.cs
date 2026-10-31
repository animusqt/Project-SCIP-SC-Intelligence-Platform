using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ScipDashboard.WPF.Converters
{
    public class MarginToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal margin)
            {
                if (margin > 20) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")); // Green - excellent
                if (margin > 10) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")); // Light green - good
                if (margin > 5) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")); // Blue - ok
                if (margin > 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")); // Amber - low
                if (margin < -5) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Red - loss
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")); // Gray - neutral
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
