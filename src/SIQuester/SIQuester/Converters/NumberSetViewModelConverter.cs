using SIPackages;
using SIQuester.ViewModel;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

[ValueConversion(typeof(QuestionTypeParam), typeof(NumberSetEditorViewModel))]
internal sealed class NumberSetViewModelConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is QuestionTypeParam questionTypeParam
            ? new NumberSetEditorViewModel(questionTypeParam.Value, value => questionTypeParam.Value = value)
            : null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
