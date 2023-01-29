using SICore;
using SIData;
using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class PersonsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return DependencyProperty.UnsetValue;
        }

        var persons = (ConnectionPersonData[])value;
        var type = (GameRole)parameter;

        var result = new StringBuilder();

        foreach (var person in persons)
        {
            if (person.Role != type || person.Name == Constants.FreePlace || !person.IsOnline)
            {
                continue;
            }

            if (result.Length > 0)
            {
                result.Append(", ");
            }

            result.Append(person.Name);
        }

        return result.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
