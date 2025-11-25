using System.Globalization;
using System.Windows.Data;
using SIQuester.ViewModel;
using SIQuester.ViewModel.Properties;

namespace SIQuester.Converters;

/// <summary>
/// Converts SortDirection enum values to localized display names.
/// </summary>
[ValueConversion(typeof(SortDirection), typeof(string))]
public sealed class SortDirectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SortDirection sortDirection)
        {
            return sortDirection switch
            {
                SortDirection.Ascending => Resources.Ascending,
                SortDirection.Descending => Resources.Descending,
                _ => value.ToString() ?? "",
            };
        }

        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException("Converting back from localized string to SortDirection is not supported.");
}