using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class ShowmanTransformConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var height = (double)value;
        var starHeight = height > 170 ? 10.0 : 30.0;

        var lineHeight = height * (starHeight / (starHeight * 2 + 35.0) + 0.075);

        return lineHeight;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
