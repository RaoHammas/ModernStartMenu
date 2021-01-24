using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ModernStartMenu_MVVM.Converters
{
    public class InvertBoolToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && (bool)value)
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && (Visibility) value == Visibility.Visible)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
