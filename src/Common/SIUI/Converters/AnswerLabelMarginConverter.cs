using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIUI.Converters;

public sealed class AnswerLabelMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (int)value switch
    {
        > 20 => new Thickness(5, 0, 5, 0),
        > 10 => new Thickness(5, 5, 5, 5),
        _ => new Thickness(20, 20, 10, 20)
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
