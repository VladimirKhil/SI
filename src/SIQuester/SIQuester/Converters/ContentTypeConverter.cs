using SIPackages.Core;
using SIPackages.Properties;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class ContentTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        ContentTypes.Text => Properties.Resources.Text,
        ContentTypes.Image => Resources.Image,
        ContentTypes.Audio => Resources.Audio,
        ContentTypes.Video => Resources.Video,
        ContentTypes.Html => Resources.Html,
        _ => value
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
