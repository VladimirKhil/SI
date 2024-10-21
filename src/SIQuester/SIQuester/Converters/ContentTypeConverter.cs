using SIPackages.Core;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class ContentTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        ContentTypes.Text => Properties.Resources.Text,
        ContentTypes.Image => ViewModel.Properties.Resources.Image,
        ContentTypes.Audio => ViewModel.Properties.Resources.Audio,
        ContentTypes.Video => ViewModel.Properties.Resources.Video,
        ContentTypes.Html => ViewModel.Properties.Resources.Html,
        _ => value
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
