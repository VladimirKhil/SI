using SImulator.Properties;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Data;

namespace SImulator.Converters;

/// <summary>
/// Converts an enum value to its localized or unlocalized description.
/// </summary>
[ValueConversion(typeof(Enum), typeof(string))]
public sealed class EnumConverter : IValueConverter
{
    public Type? EnumType { get; set; }

    public bool IsLocalized { get; set; } = true;

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var enumName = value?.ToString();

        if (enumName == null)
        {
            return null;
        }

        if (EnumType == null)
        {
            throw new InvalidOperationException("EnumType is undefined");
        }

        var field = EnumType.GetField(enumName);

        if (field == null)
        {
            return enumName;
        }

        var description = (DisplayAttribute?)Attribute.GetCustomAttribute(field, typeof(DisplayAttribute));

        return description?.Description != null ?
            (IsLocalized ? Resources.ResourceManager.GetString(description.Description) : description.Description) :
            enumName;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}
