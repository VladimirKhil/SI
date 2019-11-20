using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SImulator.Converters
{
    [ValueConversion(typeof(Enum), typeof(string))]
    public sealed class EnumConverter : IValueConverter
    {
        public Type EnumType { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            var field = EnumType.GetField(value.ToString());
            if (field == null)
                return value.ToString();

            var description = (DisplayAttribute)Attribute.GetCustomAttribute(field, typeof(DisplayAttribute));

            return description != null ? description.Description : value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
