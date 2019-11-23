using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace SIQuester.Converters
{
    public sealed class QTypeToColorConverter: IValueConverter
    {
        public Brush CommonBrush { get; set; }
        public Brush AuctionBrush { get; set; }
        public Brush CatBrush { get; set; }
        public Brush SponsoredBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            switch ((string)value)
            {
                case QuestionTypes.Auction:
                    return AuctionBrush;

                case QuestionTypes.BagCat:
                case QuestionTypes.Cat:
                    return CatBrush;

                case QuestionTypes.Sponsored:
                    return SponsoredBrush;

                default:
                    return CommonBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
