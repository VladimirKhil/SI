using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class TemplateConverter : IValueConverter
{
    public DataTemplate? DefaultTemplate { get; set; }

    public Dictionary<object, DataTemplate> Templates { get; set; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (Templates != null && Templates.TryGetValue(value, out var template))
        {
            return template;
        }

        return DefaultTemplate ?? DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
