using SIUI.ViewModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SIUI.Converters;

[ValueConversion(typeof(int), typeof(Brush))]
internal class QuestionPriceEditorConverter : IValueConverter
{
    public Brush? RemoveColor { get; set; }

    public Brush? RestoreColor { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (int)value != QuestionInfoViewModel.InvalidPrice ? RemoveColor : RestoreColor;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
