using Services.SI;
using SIGame.Properties;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIGame.Converters
{
    public sealed class SortModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is PackageSortMode))
                return DependencyProperty.UnsetValue;

            return (PackageSortMode)value == PackageSortMode.Name ? Resources.Name : Resources.PublishedDate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
