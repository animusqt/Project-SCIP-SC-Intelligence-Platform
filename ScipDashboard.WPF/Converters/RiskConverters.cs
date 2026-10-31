using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ScipDashboard.WPF.Converters
{
    public class RiskToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal margin)
            {
                if (margin >= 50) return "EXTREME";
                if (margin >= 25) return "HIGH";
                if (margin > 0) return "LOW";
                return "NONE";
            }
            return "NONE";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RiskToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal margin)
            {
                if (margin >= 50) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e74c3c")); // Red - extreme
                if (margin >= 25) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f39c12")); // Orange - high
                if (margin > 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27ae60")); // Green - low
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95a5a6")); // Gray - none
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95a5a6"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
