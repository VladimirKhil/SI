using System.Globalization;
using System.Windows.Data;

namespace SIUI.Converters;

/// <summary>
/// Allows to use provided logo uri or the default logo uri.
/// </summary>
[ValueConversion(typeof(string), typeof(string))]
public sealed class LogoConverter : IValueConverter
{
    private const string DefaultLogoUri = "/SIUI;component/Resources/logo.png";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrEmpty((string)value) ? DefaultLogoUri : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
