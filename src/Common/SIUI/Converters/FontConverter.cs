using SIUI.ViewModel.Core;
using System.Globalization;
using System.Windows.Data;

namespace SIUI.Converters;

public sealed class FontConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value as string == Settings.DefaultTableFontFamily ? "pack://application:,,,/SIUI;component/Fonts/#Futura Condensed" : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
