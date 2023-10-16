using SIGame.Properties;
using SIStorage.Service.Contract.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class NotSetTagConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var tag = (Tag)value;

        return !string.IsNullOrEmpty(tag.Name)
            ? tag.Name
            : (tag.Id == -1 ? Resources.Filter_NotSet : Resources.Filter_All);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
