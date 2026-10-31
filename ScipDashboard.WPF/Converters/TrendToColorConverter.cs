using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ScipDashboard.WPF.Converters
{
    public class TrendToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string trend)
            {
                return trend switch
                {
                    "Strong Buy" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")),
                    "Weak Buy" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")),
                    "Marginal" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                    "Avoid" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"))
                };
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
