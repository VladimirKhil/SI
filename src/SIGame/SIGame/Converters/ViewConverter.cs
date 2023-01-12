using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class ViewConverter : IValueConverter
{
    public Dictionary<object, DataTemplate> Views { get; set; } = new();

    public DataTemplate? DefaultView { get; set; }

    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null && Views.TryGetValue(value, out var template))
        {
            return template;
        }

        return DefaultView;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
