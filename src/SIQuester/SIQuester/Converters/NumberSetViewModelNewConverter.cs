using SIPackages.Core;
using SIQuester.ViewModel;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

[ValueConversion(typeof(NumberSet), typeof(NumberSetEditorNewViewModel))]
internal sealed class NumberSetViewModelNewConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is NumberSet numberSet ? new NumberSetEditorNewViewModel(numberSet) : null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
