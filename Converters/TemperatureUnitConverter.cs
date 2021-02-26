using System;
using System.Globalization;
using System.Windows.Data;

namespace ModernStartMenu_MVVM.Converters
{
    public class TemperatureUnitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && !string.IsNullOrEmpty(value.ToString()))
            {
                var unit = value.ToString();
                return unit?[0];
            } //return first character

            return "C"; //default return C
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
