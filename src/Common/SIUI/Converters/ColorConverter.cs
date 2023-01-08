using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SIUI.Converters;

[ValueConversion(typeof(string), typeof(Color))]
public sealed class ColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var colorString = (string)value;
        var color = (Color)System.Windows.Media.ColorConverter.ConvertFromString(colorString);
        // Осветлим
        ColorToHSV(color, out double hue, out double saturation, out double val);
        val = Math.Min(1.0, val + 0.36);

        return ColorFromHSV(hue, saturation, val);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();

    public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
    {
        var max = Math.Max(color.R, Math.Max(color.G, color.B));
        var min = Math.Min(color.R, Math.Min(color.G, color.B));

        hue = max == min ? 0 :
            max == color.R && color.G >= color.B ? 60d * (color.G - color.B) / (max - min) :
            max == color.R && color.G < color.B ? 60d * (color.G - color.B) / (max - min) + 360d :
            max == color.G ? 60d * (color.B - color.R) / (max - min) + 120d :
            60d * (color.R - color.G) / (max - min) + 240d;

        saturation = (max == 0) ? 0 : 1d - (1d * min / max);
        value = max / 255d;
    }

    public static Color ColorFromHSV(double hue, double saturation, double value)
    {
        int hi = System.Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        value *= 255;
        var v = System.Convert.ToByte(value);
        var p = System.Convert.ToByte(value * (1 - saturation));
        var q = System.Convert.ToByte(value * (1 - f * saturation));
        var t = System.Convert.ToByte(value * (1 - (1 - f) * saturation));

        if (hi == 0)
            return Color.FromArgb(255, v, t, p);
        else if (hi == 1)
            return Color.FromArgb(255, q, v, p);
        else if (hi == 2)
            return Color.FromArgb(255, p, v, t);
        else if (hi == 3)
            return Color.FromArgb(255, p, q, v);
        else if (hi == 4)
            return Color.FromArgb(255, t, p, v);
        else
            return Color.FromArgb(255, v, p, q);
    }
}
