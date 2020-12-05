using SIGame.Properties;
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Data;

namespace SIGame.Converters
{
    [ValueConversion(typeof(Enum), typeof(string))]
    public sealed class EnumConverter: IValueConverter
    {
        public Type EnumType { get; set; }
        public bool IsLocalized { get; set; } = true;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
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

            var description = (DisplayAttribute)Attribute.GetCustomAttribute(field, typeof(DisplayAttribute));

            return description != null ?
                (IsLocalized ? Resources.ResourceManager.GetString(description.Description) : description.Description) :
                value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
