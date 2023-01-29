using SIGame.Properties;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class SortDirectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool)
        {
            return DependencyProperty.UnsetValue;
        }

        return (bool)value ? Resources.Ascending: Resources.Descending;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
