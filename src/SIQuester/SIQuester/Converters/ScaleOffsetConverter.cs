using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

/// <summary>
/// Returns value * scale + offset (never below zero). Parameter format: "scale offset", e.g. "0.85 -64".
/// </summary>
public sealed class ScaleOffsetConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var parts = (parameter?.ToString() ?? "1 0").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var scale = parts.Length > 0 ? double.Parse(parts[0], CultureInfo.InvariantCulture) : 1.0;
        var offset = parts.Length > 1 ? double.Parse(parts[1], CultureInfo.InvariantCulture) : 0.0;

        // Fallback until the layout is known: "auto" (NaN) for Width, "no limit" (Infinity) for MaxWidth
        var fallback = parts.Length > 2 && parts[2] == "auto" ? double.NaN : double.PositiveInfinity;

        var result = System.Convert.ToDouble(value) * scale + offset;
        return result > 0 ? result : fallback;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
