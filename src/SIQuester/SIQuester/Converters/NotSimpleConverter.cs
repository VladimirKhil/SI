using SIPackages.Core;
using System;
using System.Windows.Data;

namespace SIQuester.Converters
{
    public sealed class NotSimpleConverter: IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !string.Equals(value.ToString(), QuestionTypes.Simple);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

        #endregion
    }
}
