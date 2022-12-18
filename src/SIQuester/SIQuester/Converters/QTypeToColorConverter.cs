using SIPackages.Core;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SIQuester.Converters;

public sealed class QTypeToColorConverter : IValueConverter
{
    public Brush? CommonBrush { get; set; }

    public Brush? AuctionBrush { get; set; }

    public Brush? CatBrush { get; set; }

    public Brush? SponsoredBrush { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (string)value switch
        {
            QuestionTypes.Auction => AuctionBrush,
            QuestionTypes.BagCat or QuestionTypes.Cat => CatBrush,
            QuestionTypes.Sponsored => SponsoredBrush,
            _ => CommonBrush
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
