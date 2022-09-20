using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;

namespace SIUI.Converters
{
    public sealed class TemplateConverter : IValueConverter
    {
        public DataTemplate DefaultTemplate { get; set; }
        public Dictionary<object, DataTemplate> Templates { get; set; }

        public TemplateConverter()
        {
            Templates = new Dictionary<object, DataTemplate>();
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (Templates != null && Templates.TryGetValue(value, out DataTemplate template))
                return template;

            return DefaultTemplate ?? DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
