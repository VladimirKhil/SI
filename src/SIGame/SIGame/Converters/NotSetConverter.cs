using SIGame.Properties;
using SIStorageService.Client.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class NotSetConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var named = (NamedObject)value;
        return named.Name ?? (named.ID == -1 ? Resources.Filter_NotSet : Resources.Filter_All);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
