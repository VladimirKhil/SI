using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class LanguageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value.ToString() == "en-US" ? "English" : "Русский";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
