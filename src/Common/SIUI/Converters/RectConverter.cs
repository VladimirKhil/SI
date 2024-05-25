using SIUI.ViewModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIUI.Converters;

public sealed class RectConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] == DependencyProperty.UnsetValue
            || values[0] == null
            || values[1] == DependencyProperty.UnsetValue
            || values[2] == DependencyProperty.UnsetValue)
        {
            return new Rect();
        }

        var partialImageVisibility = (double)values[0];
        var imageWidth = (double)values[1];
        var imageHeight = (double)values[2];

        return new Rect(0, 0, imageWidth, imageHeight * partialImageVisibility);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
