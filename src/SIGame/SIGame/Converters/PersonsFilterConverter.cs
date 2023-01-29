using SICore;
using SIData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class PersonsFilterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return DependencyProperty.UnsetValue;
        }

        var persons = (ConnectionPersonData[])value;
        var type = (GameRole)parameter;

        var result = new List<string>();

        foreach (var person in persons)
        {
            if (person.Role != type || person.Name == Constants.FreePlace || !person.IsOnline)
            {
                continue;
            }

            result.Add(person.Name);
        }

        return result.ToArray();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
