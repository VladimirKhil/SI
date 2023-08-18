using SIPackages.Core;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SIQuester.Converters;

[ValueConversion(typeof(string), typeof(Brush))]
public sealed class QTypeToColorConverter : IValueConverter
{
    public Brush? CommonBrush { get; set; }

    public Brush? AuctionBrush { get; set; }

    public Brush? CatBrush { get; set; }

    public Brush? SponsoredBrush { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (string)value switch
        {
            QuestionTypes.Auction or QuestionTypes.Stake => AuctionBrush,
            QuestionTypes.BagCat or QuestionTypes.Cat or QuestionTypes.Secret or QuestionTypes.SecretPublicPrice or QuestionTypes.SecretNoQuestion => CatBrush,
            QuestionTypes.Sponsored or QuestionTypes.NoRisk => SponsoredBrush,
            _ => CommonBrush
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
