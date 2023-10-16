using SIQuester.Properties;
using SIStorage.Service.Contract.Models;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class NameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var namedObject = (Publisher)value;

        return namedObject == null || namedObject.Id == -1
            ? Resources.PublishersNotSet
            : namedObject.Id == -2 ? Resources.PublishersAll : namedObject.Name;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
