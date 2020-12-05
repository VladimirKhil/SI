using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace SIQuester.Converters
{
    public sealed class UnionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var result = new CommandBindingCollection();
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == DependencyProperty.UnsetValue)
                    continue;
                result.AddRange(values[i] as CommandBindingCollection);
            }
            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
