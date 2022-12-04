using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace SImulator.Converters;

public sealed class KeyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => ((Key)value).ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
