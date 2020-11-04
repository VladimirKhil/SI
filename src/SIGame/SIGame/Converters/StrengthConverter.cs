using System;
using System.Windows.Data;
using SICore;
using SIGame.Properties;

namespace SIGame.Converters
{
    [ValueConversion(typeof(ComputerAccount), typeof(string))]
    public sealed class StrengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is ComputerAccount account))
                return null;

            if (account.F > 170)
                return Resources.Strength_High;
            else if (account.F > 130)
                return Resources.Strength_Middle;
            else if (account.F > 10)
                return Resources.Strength_Low;
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
