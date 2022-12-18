using SIQuester.Model;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

[ValueConversion(typeof(Enum), typeof(string))]
public sealed class PackageTypeNameConverter : IValueConverter
{
    public Type? EnumType { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
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

        var packageTypeName = (PackageTypeNameAttribute?)Attribute.GetCustomAttribute(field, typeof(PackageTypeNameAttribute));

        return packageTypeName != null ? packageTypeName.Name : value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
