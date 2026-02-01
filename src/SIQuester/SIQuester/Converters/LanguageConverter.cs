using SIQuester.Properties;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

/// <summary>
/// Converts culture code into readable language name in this language.
/// </summary>
[ValueConversion(typeof(string), typeof(string))]
public sealed class LanguageConverter : IValueConverter
{
    private static readonly Dictionary<string, string> LanguageNames = new()
    {
        { "ru-RU", "Русский" },
        { "sr-RS", "Srpski" },
        { "other", Resources.OtherLanguage }
    };

    private static readonly string DefaultLanguage = "English";

    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is string str && LanguageNames.TryGetValue(str, out var name) ? name : DefaultLanguage;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
