using SImulator.ViewModel.Core;
using System;
using System.Windows.Data;

namespace SImulator.Converters
{
    public sealed class PlayersConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (PlayersViewMode)value != PlayersViewMode.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? PlayersViewMode.Visible : PlayersViewMode.Hidden;
        }
    }
}
