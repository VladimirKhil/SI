using SIPackages.Core;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class NotSimpleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        !string.Equals(value.ToString(), QuestionTypes.Simple);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}
