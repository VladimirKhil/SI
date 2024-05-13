using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class IsValidLinkConverter : IValueConverter
{
    private static readonly string[] ValidLinks = new string[] { "https://vk.com/", "https://t.me/", "https://discord.com/", "https://discord.gg/", "https://www.twitch.tv/" };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        ValidLinks.Any(link => ((string)value).StartsWith(link));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
