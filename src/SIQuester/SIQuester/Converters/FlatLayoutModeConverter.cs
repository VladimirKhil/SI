using SIQuester.Model;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

/// <summary>
/// Converts Flat Layout mode to boolean and back.
/// </summary>
[ValueConversion(typeof(FlatLayoutMode), typeof(bool))]
internal sealed class FlatLayoutModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (FlatLayoutMode)value == FlatLayoutMode.Table;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        (bool)value ? FlatLayoutMode.Table : FlatLayoutMode.List;
}
