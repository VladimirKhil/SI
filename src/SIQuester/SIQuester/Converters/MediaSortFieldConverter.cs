using System.Globalization;
using System.Windows.Data;
using SIQuester.ViewModel;
using SIQuester.ViewModel.Properties;

namespace SIQuester.Converters;

/// <summary>
/// Converts MediaSortField enum values to localized display names.
/// </summary>
[ValueConversion(typeof(MediaSortField), typeof(string))]
public sealed class MediaSortFieldConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MediaSortField sortField)
        {
            return sortField switch
            {
                MediaSortField.Name => Resources.Name,
                MediaSortField.Size => Resources.Size,
                _ => value.ToString() ?? "",
            };
        }

        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException("Converting back from localized string to MediaSortField is not supported.");
}