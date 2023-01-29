using SIGame.Properties;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class FontFamilyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string fontFamily)
        {
            return DependencyProperty.UnsetValue;
        }

        if (fontFamily == "_Default")
        {
            return Resources.Default;
        }

        return fontFamily;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
