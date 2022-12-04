using System;
using System.Windows.Data;
using System.Windows;
using SIUI.ViewModel.Core;
using System.Globalization;

namespace SImulator.Converters;

public sealed class FontFamilyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string fontFamily)
        {
            return DependencyProperty.UnsetValue;
        }

        if (fontFamily == Settings.DefaultTableFontFamily || fontFamily.StartsWith("pack:"))
        {
            return "(по умолчанию)";
        }

        return fontFamily;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
