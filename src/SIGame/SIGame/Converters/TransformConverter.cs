using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace SIGame.Converters
{
    public sealed class TransformConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new TranslateTransform(((FrameworkElement)values[1]).ActualWidth * 0.6 * ((int)values[0] == 0 ? 1 : -1), 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
