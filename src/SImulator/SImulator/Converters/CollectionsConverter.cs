using System;
using System.Text;
using System.Windows.Data;
using System.Collections;
using System.Globalization;

namespace SImulator.Converters;

[ValueConversion(typeof(IEnumerable), typeof(string))]
public sealed class CollectionsConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IEnumerable sources)
        {
            return null;
        }

        var result = new StringBuilder();

        foreach (var item in sources)
        {
            if (result.Length > 0)
            {
                result.Append(", ");
            }

            result.Append(item);
        }

        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
