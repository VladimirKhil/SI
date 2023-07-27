using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

[ValueConversion(typeof(TimeSpan), typeof(int))]
public sealed class TimeSpanToSecondsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (int)((TimeSpan)value).TotalSeconds;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        TimeSpan.FromSeconds(System.Convert.ToDouble(value));
}
