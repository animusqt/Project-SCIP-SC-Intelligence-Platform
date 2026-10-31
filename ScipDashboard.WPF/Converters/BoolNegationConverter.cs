using System;
using System.Globalization;
using System.Windows.Data;

namespace ScipDashboard.WPF.Converters
{
    /// <summary>
    /// Inverts a boolean value. Useful for binding IsEnabled to !IsProcessing.
    /// </summary>
    public class BoolNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return true;
        }
    }
}
