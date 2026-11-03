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
                    "Up" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")),
                    "Down" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
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
