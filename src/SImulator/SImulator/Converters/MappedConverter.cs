using System;
using System.Globalization;
using System.Windows.Data;

namespace SImulator.Converters;

public sealed class MappedConverter : IValueConverter
{
    public StringDictionary? Map { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        if (Map != null && Map.TryGetValue(value.ToString() ?? "", out var result))
        {
            return result;
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Map == null)
        {
            return value;
        }

        var val = value?.ToString();

        foreach (var item in Map)
        {
            if (item.Value == val)
            {
                return item.Key;
            }
        }

        return value;
    }
}
