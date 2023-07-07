using SIPackages.Core;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class NotDefaultTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        !string.Equals(value.ToString(), QuestionTypes.Default);

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}
