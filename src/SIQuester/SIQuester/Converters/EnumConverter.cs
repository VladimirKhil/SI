using SIQuester.Properties;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

[ValueConversion(typeof(Enum), typeof(string))]
public sealed class EnumConverter : IValueConverter
{
    public Type? EnumType { get; set; }

    public bool IsLocalized { get; set; } = true;

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (EnumType == null)
        {
            throw new InvalidOperationException("EnumType is undefined");
        }

        if (value == null)
        {
            return null;
        }

        var field = EnumType.GetField(value.ToString() ?? "");

        if (field == null)
        {
            return value.ToString();
        }

        var description = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));

        return description != null ?
            (IsLocalized ? Resources.ResourceManager.GetString(description.Description ?? "")
                ?? ViewModel.Properties.Resources.ResourceManager.GetString(description.Description ?? "") : description.Description) :
            value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}
