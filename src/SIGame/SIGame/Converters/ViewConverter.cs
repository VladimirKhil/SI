using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;

namespace SIGame.Converters
{
    public sealed class ViewConverter: IValueConverter
    {
        public Dictionary<object, DataTemplate> Views { get; set; }
        public DataTemplate DefaultView { get; set; }

        public ViewConverter()
        {
            Views = new Dictionary<object, DataTemplate>();
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (Views.TryGetValue(value, out var result))
            {
                return result;
            }

            return DefaultView;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }
}
