using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace SIGame.Converters;

[ValueConversion(typeof(FrameworkElement), typeof(Thickness))]
public sealed class MarginConverter : IValueConverter
{
    public Size BaseSize { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var element = value as FrameworkElement;

        if (element == null || parameter == null)
        {
            return new Thickness(0);
        }

        var defaultThickness = (Thickness)parameter;
        var coefx = element.ActualWidth / BaseSize.Width;
        var coefy = element.ActualHeight / BaseSize.Height;

        return new Thickness(defaultThickness.Left * coefx,
            defaultThickness.Top * coefy,
            defaultThickness.Right * coefx,
            defaultThickness.Bottom * coefy);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
