using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIQuester.Converters;

/// <summary>
/// Converts a <see langword="false"/> value to <see cref="Visibility.Visible"/> and a <see langword="true"/> value to
/// <see cref="Visibility.Collapsed"/>.
/// </summary>
/// <remarks>This converter is typically used in data binding scenarios where a <see cref="bool"/> value needs to
/// control the visibility of a UI element. The conversion logic assumes the input value is of type <see
/// cref="bool"/>.</remarks>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class FalseToVisiblityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (bool)value ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => 
        throw new NotImplementedException();
}
