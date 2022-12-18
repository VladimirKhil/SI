using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

[ValueConversion(typeof(Enum), typeof(string))]
public sealed class EnumConverter : IValueConverter
{
    public Type? EnumType { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        var field = EnumType.GetField(value.ToString());

        if (field == null)
        {
            return value.ToString();
        }

        var description = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));

        return description != null ? description.Description : value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}
