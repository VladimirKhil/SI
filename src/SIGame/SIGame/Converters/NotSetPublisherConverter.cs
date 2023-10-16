using SIGame.Properties;
using SIStorage.Service.Contract.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class NotSetPublisherConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var publisher = (Publisher)value;

        return !string.IsNullOrEmpty(publisher.Name) 
            ? publisher.Name
            : (publisher.Id == -1 ? Resources.Filter_NotSet : Resources.Filter_All);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
