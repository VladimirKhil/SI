using SImulator.ViewModel.Core;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SImulator.Converters;

public sealed class PlayersConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (PlayersViewMode)value != PlayersViewMode.Hidden;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        (bool)value ? PlayersViewMode.Visible : PlayersViewMode.Hidden;
}
