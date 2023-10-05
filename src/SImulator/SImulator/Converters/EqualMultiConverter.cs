using System;
using System.Globalization;
using System.Windows.Data;

namespace SImulator.Converters;

/// <summary>
/// Compares two values for equality.
/// </summary>
internal sealed class EqualMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2)
        {
            throw new InvalidOperationException($"values.Length != 2 ({string.Join(", ", values)})");
        }

        return Equals(values[0], values[1]);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
