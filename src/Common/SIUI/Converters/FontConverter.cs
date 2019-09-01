using SIUI.ViewModel.Core;
using System;
using System.Windows.Data;

namespace SIUI.Converters
{
    public sealed class FontConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value as string == Settings.DefaultTableFontFamily ? "pack://application:,,,/SIUI;component/Fonts/#Futura Condensed" : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
