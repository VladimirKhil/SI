using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class DialogPositionConverter : IMultiValueConverter
{
    private const int BottomMargin = 42;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
        {
            return DependencyProperty.UnsetValue;
        }

        var dialogHeight = (double)values[0];
        var totalHeight = (double)values[1];

        var bottomHeight = totalHeight * 10 / 26 - BottomMargin;

        if (dialogHeight <= bottomHeight)
        {
            return new Thickness(0, 0, 0, BottomMargin + (bottomHeight - dialogHeight) / 2);
        }

        return new Thickness(0, 0, 0, BottomMargin);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
