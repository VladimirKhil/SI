using System;
using System.Windows.Data;
using System.Windows;
using SIUI.ViewModel.Core;

namespace SImulator.Converters
{
    public sealed class FontFamilyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var fontFamily = value as string;
            if (fontFamily == null)
                return DependencyProperty.UnsetValue;

            if (fontFamily == Settings.DefaultTableFontFamily || fontFamily.StartsWith("pack:"))
                return "(по умолчанию)";

            return fontFamily;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
