using System;
using System.Windows.Data;

namespace SIUI.Converters
{
    public sealed class ZoomConverter : IMultiValueConverter
    {
        public double BaseWidth { get; set; }
        public double BaseHeight { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var width = (double)values[0];
            var height = (double)values[1];

            return Math.Min(width / BaseWidth, height / BaseHeight);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
