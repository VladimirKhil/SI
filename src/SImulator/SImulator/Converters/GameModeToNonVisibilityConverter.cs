using System;
using System.Windows.Data;
using System.Windows;
using SImulator.ViewModel.Core;
using System.Globalization;

namespace SImulator.Converters;

public sealed class GameModeToNonVisibilityConverter : IValueConverter
{
    public GameMode Mode { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Mode != (GameMode)value ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
