using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;

namespace SIQuester.Converters
{
    public sealed class ListJoinConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var result = new List<object>();

            foreach (var item in values)
            {
                if (item == DependencyProperty.UnsetValue)
                    continue;

                result.AddRange((IEnumerable<object>)item);
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
