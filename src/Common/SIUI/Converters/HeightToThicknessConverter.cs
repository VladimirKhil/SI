using System;
using System.Windows.Data;
using System.Windows;

namespace SIUI.Converters
{
    public sealed class HeightToThicknessConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new Thickness(0, System.Convert.ToDouble(value) * (bool.Parse(parameter.ToString()) ? 1 : -1), 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
