using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class PlayerIndiciesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Enumerable.Range(0, (int)value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
