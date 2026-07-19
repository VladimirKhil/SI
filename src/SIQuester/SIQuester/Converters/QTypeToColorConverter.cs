using SIPackages.Core;
using SIQuester.Model;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SIQuester.Converters;

[ValueConversion(typeof(object[]), typeof(Brush))]
public sealed class QTypeToColorConverter : IMultiValueConverter
{
    public Brush? CommonBrush { get; set; }

    public Brush? CommonBrushLight { get; set; }

    public Brush? CommonBrushDark { get; set; }

    public Brush? CommonBrushDarkGray { get; set; }

    public Brush? StakeBrush { get; set; }

    public Brush? SecretBrush { get; set; }

    public Brush? NoRiskBrush { get; set; }

    public Brush? StakeAllBrush { get; set; }

    public Brush? ForAllBrush { get; set; }

    private Brush? GetThemeAwareCommonBrush(ThemeOption theme)
    {
        return theme switch
        {
            ThemeOption.Light => CommonBrushLight ?? CommonBrush,
            ThemeOption.Dark => CommonBrushDark ?? CommonBrush,
            ThemeOption.DarkGray => CommonBrushDarkGray ?? CommonBrush,
            _ => CommonBrush
        };
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not string questionType)
        {
            return CommonBrush ?? Brushes.Black;
        }

        var theme = values.Length > 1 && values[1] is ThemeOption themeOption ? themeOption : ThemeOption.Light;

        return questionType switch
        {
            QuestionTypes.Stake => StakeBrush ?? Brushes.Black,
            QuestionTypes.Secret or QuestionTypes.SecretPublicPrice or QuestionTypes.SecretNoQuestion => SecretBrush ?? Brushes.Black,
            QuestionTypes.NoRisk => NoRiskBrush ?? Brushes.Black,
            QuestionTypes.StakeAll => StakeAllBrush ?? Brushes.Black,
            QuestionTypes.ForAll => ForAllBrush ?? Brushes.Black,
            _ => GetThemeAwareCommonBrush(theme) ?? Brushes.Black
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
