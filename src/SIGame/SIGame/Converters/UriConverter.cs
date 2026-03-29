using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class UriConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Uri.TryCreate(value.ToString() ?? "", UriKind.RelativeOrAbsolute, out var uri) ? uri : null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
