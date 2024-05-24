using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

/// <summary>
/// Converts culture code into readable language name in this language.
/// </summary>
[ValueConversion(typeof(string), typeof(string))]
public sealed class LanguageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value?.ToString() == "en-US" ? "English" : "Русский";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
