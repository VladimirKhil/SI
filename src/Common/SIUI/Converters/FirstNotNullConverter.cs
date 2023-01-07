using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIUI.Converters;

public sealed class FirstNotNullConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        foreach (var item in values)
        {
            if (item != null && item != DependencyProperty.UnsetValue)
            {
                return item;
            }
        }

        return parameter;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
