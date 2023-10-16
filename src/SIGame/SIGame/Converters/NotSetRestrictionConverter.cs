using SIGame.Properties;
using SIStorage.Service.Contract.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class NotSetRestrictionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var restriction = (Restriction)value;

        return !string.IsNullOrEmpty(restriction.Value)
            ? restriction.Value
            : (restriction.Id == -1 ? Resources.Filter_NotSet : Resources.Filter_All);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
