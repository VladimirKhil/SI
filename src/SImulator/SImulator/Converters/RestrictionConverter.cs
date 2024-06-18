using SImulator.Properties;
using SIStorage.Service.Contract.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SImulator.Converters;

public sealed class RestrictionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        var restriction = (Restriction)value;
        return restriction.Id == -2 ? Resources.All : (restriction.Id == -1 ? Resources.NotSet : restriction.Value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => 
        throw new NotImplementedException();
}
