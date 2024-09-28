using SIPackages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace SImulator.Converters;

public sealed class IncludeConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not IReadOnlyCollection<ContentItem> collection)
        {
            return false;
        }

        var value = values[1] as ContentItem;

        foreach (var item in collection)
        {
            if (item == value)
            {
                return true;
            }
        }

        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
