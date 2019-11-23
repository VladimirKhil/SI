using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using YourGame.SIPackages;
using System.Windows;

namespace SIQuester.Converters
{
    [ValueConversion(typeof(InfoOwner), typeof(ContextualCommand))]
    public sealed class InfoOwnerToContextualCommandConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var infoOwner = value as InfoOwner;
            if (infoOwner == null)
                return DependencyProperty.UnsetValue;

            return new ContextualCommand { Header = infoOwner.Name };
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
