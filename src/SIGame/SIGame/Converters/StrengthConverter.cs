using SIData;
using SIGame.Properties;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters
{
    [ValueConversion(typeof(ComputerAccount), typeof(string))]
    public sealed class StrengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
