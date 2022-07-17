using System;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters
{
    public sealed class DirectoryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            System.IO.Path.GetDirectoryName(value.ToString());

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
