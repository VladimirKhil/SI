using System;
using System.Windows.Data;
using System.Windows;

namespace SIGame.Converters
{
    [ValueConversion(typeof(object), typeof(Visibility))]
    public sealed class IsTypeConverter : IValueConverter
    {
        public Type DataType { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value != null && (value.GetType() == DataType || value.GetType().IsSubclassOf(DataType)) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
