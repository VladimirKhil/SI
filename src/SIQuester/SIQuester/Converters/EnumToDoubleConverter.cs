using SIQuester.Model;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class EnumToDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (double)(int)value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => (FlatScale)(int)Math.Round((double)value);
}
