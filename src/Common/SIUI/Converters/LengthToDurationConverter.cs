using System;
using System.Windows.Data;
using System.Windows;

namespace SIUI.Converters
{
    /// <summary>
    /// Вычисляет, сколько времени нужно потратить на чтение текста исходя из длины текста
    /// </summary>
    [ValueConversion(typeof(int), typeof(Duration))]
    public sealed class LengthToDurationConverter : IMultiValueConverter//IValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return DependencyProperty.UnsetValue;

            var length = System.Convert.ToInt32(values[0]);
            var speed = System.Convert.ToDouble(values[1]);

            return new Duration(TimeSpan.FromSeconds(length * speed));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
