using SImulator.ViewModel.Core;
using System;
using System.Windows.Data;

namespace SImulator.Converters
{
    public sealed class GameModeToBooleanConverter : IValueConverter
    {
        public GameMode Mode { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Mode == (GameMode)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
