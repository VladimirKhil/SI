using SIQuester.Model;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters
{
    public sealed class EnumToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)(int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (FlatScale)(int)Math.Round((double)value);
        }
    }
}
