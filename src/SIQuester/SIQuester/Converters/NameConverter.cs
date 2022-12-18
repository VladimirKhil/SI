using SIQuester.Properties;
using SIStorageService.Client.Models;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class NameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var namedObject = (NamedObject)value;

        return namedObject == null || namedObject.ID == -1
            ? Resources.PublishersNotSet
            : namedObject.ID == -2 ? Resources.PublishersAll : namedObject.Name;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
