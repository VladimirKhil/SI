using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

/// <summary>
/// Converts file size from bytes to kilobytes with appropriate formatting.
/// </summary>
[ValueConversion(typeof(long), typeof(string))]
public sealed class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long sizeInBytes)
        {
            if (sizeInBytes < 0)
            {
                return "-";
            }

            double sizeInKb = sizeInBytes / 1024.0;
            return sizeInKb.ToString("F1", culture); // Format with 1 decimal place
        }

        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException("Converting back from KB to bytes is not supported.");
}