using SIStorageService.Client.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SImulator.Converters;

public sealed class NameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var publisher = (NamedObject)value;
        return publisher.ID == -2 ? "(все)" : (publisher.ID == -1 ? "(не задано)" : publisher.Name);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => 
        throw new NotImplementedException();
}
