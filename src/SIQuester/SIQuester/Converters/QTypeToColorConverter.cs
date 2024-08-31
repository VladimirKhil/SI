using SIPackages.Core;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SIQuester.Converters;

[ValueConversion(typeof(string), typeof(Brush))]
public sealed class QTypeToColorConverter : IValueConverter
{
    public Brush? CommonBrush { get; set; }

    public Brush? StakeBrush { get; set; }

    public Brush? SecretBrush { get; set; }

    public Brush? NoRiskBrush { get; set; }

    public Brush? StakeAllBrush { get; set; }

    public Brush? ForAllBrush { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (string)value switch
        {
            QuestionTypes.Stake => StakeBrush,
            QuestionTypes.Secret or QuestionTypes.SecretPublicPrice or QuestionTypes.SecretNoQuestion => SecretBrush,
            QuestionTypes.NoRisk => NoRiskBrush,
            QuestionTypes.StakeAll => StakeAllBrush,
            QuestionTypes.ForAll => ForAllBrush,
            _ => CommonBrush
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
