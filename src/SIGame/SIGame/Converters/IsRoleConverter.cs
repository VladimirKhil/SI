using System;
using System.Windows.Data;
using SICore;

namespace SIGame.Converters
{
    [ValueConversion(typeof(GameRole), typeof(bool))]
    public sealed class IsRoleConverter : IValueConverter
    {
        public GameRole Role { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (GameRole)value == Role;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
