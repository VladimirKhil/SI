using SIQuester.ViewModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIQuester.Converters;

[ValueConversion(typeof(ImportTextViewModel.UIState), typeof(DataTemplate))]
internal sealed class ImportTextTemplateConverter : IValueConverter
{
    public DataTemplate? InitialTemplate { get; set; }

    public DataTemplate? ImportFileTemplate { get; set; }

    public DataTemplate? SplitTemplate { get; set; }

    public DataTemplate? ParseTemplate { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (ImportTextViewModel.UIState)value switch
        {
            ImportTextViewModel.UIState.Initial => InitialTemplate,
            ImportTextViewModel.UIState.ImportFile => ImportFileTemplate,
            ImportTextViewModel.UIState.Split => SplitTemplate,
            ImportTextViewModel.UIState.Parse => ParseTemplate,
            _ => null,
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
