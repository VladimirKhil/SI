using SIUI.ViewModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SIUI.Converters;

/// <summary>
/// Converts state into color.
/// </summary>
public sealed class StateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ItemState state)
        {
            return DependencyProperty.UnsetValue;
        }

        return state switch
        {
            ItemState.Normal => Brushes.Transparent,
            ItemState.Active => Brushes.LightYellow,
            ItemState.Right => Brushes.Green,
            ItemState.Wrong => Brushes.DarkRed,
            _ => Brushes.Transparent,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
