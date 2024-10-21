using SImulator.Properties;
using SIPackages.Core;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SImulator.Converters;

/// <summary>
/// Converts atom type to localized name.
/// </summary>
[ValueConversion(typeof(string), typeof(string))]
public sealed class AtomTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var localizedName = GetLocalizedName((string)value);
        return localizedName.Length == 0 ? "" : localizedName + ' ';
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();

    private static string GetLocalizedName(string value) =>
        value switch
        {
            ContentTypes.Text => "",
            ContentTypes.Image => Resources.Image,
            ContentTypes.Audio => Resources.Audio,
            ContentTypes.Video => Resources.Video,
            ContentTypes.Html => Resources.Html,
            _ => Resources.UnknownType,
        };
}
